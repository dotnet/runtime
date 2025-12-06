// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;
using System.Collections.Generic;
using System.Diagnostics;
using ILCompiler;

#nullable enable

namespace Internal.IL.Stubs
{
    public static class AsyncThunkILEmitter
    {
        public struct TaskReturningThunkReferences
        {
            public required MethodDesc ExecutionAndSyncBlockStore_Push { get; init; }
            public required MethodDesc AsyncCallContinuation { get; init; }
            public required MethodDesc GetResultOrCompletedTask { get; init; }
            public required MethodDesc TaskFromException { get; init; }
            public required MethodDesc FinalizeTaskReturningThunk { get; init; }
            public required MethodDesc ExecutionAndSyncBlockStore_Pop { get; init; }
            public required TypeDesc LogicalReturnType { get; init; }
            public required TypeDesc ExecutionAndSyncBlockStore { get; init; }
            public required TypeDesc Exception { get; init; }

            public IEnumerable<TypeSystemEntity> GetAllReferences()
            {
                yield return ExecutionAndSyncBlockStore_Push;
                yield return AsyncCallContinuation;
                yield return GetResultOrCompletedTask;
                yield return TaskFromException;
                yield return FinalizeTaskReturningThunk;
                yield return ExecutionAndSyncBlockStore_Pop;
                yield return LogicalReturnType;
                yield return ExecutionAndSyncBlockStore;
            }
        }

        private static TaskReturningThunkReferences GetTaskReturningThunkMethods(MethodDesc taskReturningMethod)
        {
            TypeSystemContext context = taskReturningMethod.Context;

            TypeDesc returnType = taskReturningMethod.Signature.ReturnType;
            TypeDesc logicalReturnType = returnType.HasInstantiation ?
                returnType.Instantiation[0]
                : context.GetWellKnownType(WellKnownType.Void);
            bool isValueTask = returnType.IsValueType;

            MethodDesc getResultOrCompletedTask;
            MethodDesc taskFromException;
            MethodDesc finalizeTask;
            if (!logicalReturnType.IsVoid)
            {
                if (isValueTask)
                {
                    getResultOrCompletedTask = KnownILStubReferences.GetKnownMethod(context, KnownILStubReference.ValueTask_FromResult_1)
                        .MakeInstantiatedMethod(new Instantiation(logicalReturnType));
                    taskFromException = KnownILStubReferences.GetKnownMethod(context, KnownILStubReference.AsyncHelpers_ValueTaskFromException_1)
                        .MakeInstantiatedMethod(new Instantiation(logicalReturnType));
                    finalizeTask = KnownILStubReferences.GetKnownMethod(context, KnownILStubReference.AsyncHelpers_FinalizeValueTaskReturningThunk_1)
                        .MakeInstantiatedMethod(new Instantiation(logicalReturnType));
                }
                else
                {
                    getResultOrCompletedTask = KnownILStubReferences.GetKnownMethod(context, KnownILStubReference.Task_FromResult_1)
                        .MakeInstantiatedMethod(new Instantiation(logicalReturnType));
                    taskFromException = KnownILStubReferences.GetKnownMethod(context, KnownILStubReference.AsyncHelpers_TaskFromException_1)
                        .MakeInstantiatedMethod(new Instantiation(logicalReturnType));
                    finalizeTask = KnownILStubReferences.GetKnownMethod(context, KnownILStubReference.AsyncHelpers_FinalizeTaskReturningThunk_1)
                        .MakeInstantiatedMethod(new Instantiation(logicalReturnType));
                }
            }
            else
            {
                if (isValueTask)
                {
                    getResultOrCompletedTask = KnownILStubReferences.GetKnownMethod(context, KnownILStubReference.ValueTask_get_CompletedTask);
                    taskFromException = KnownILStubReferences.GetKnownMethod(context, KnownILStubReference.AsyncHelpers_ValueTaskFromException);
                    finalizeTask = KnownILStubReferences.GetKnownMethod(context, KnownILStubReference.AsyncHelpers_FinalizeValueTaskReturningThunk);
                }
                else
                {
                    getResultOrCompletedTask = KnownILStubReferences.GetKnownMethod(context, KnownILStubReference.Task_get_CompletedTask);
                    taskFromException = KnownILStubReferences.GetKnownMethod(context, KnownILStubReference.AsyncHelpers_TaskFromException);
                    finalizeTask = KnownILStubReferences.GetKnownMethod(context, KnownILStubReference.AsyncHelpers_FinalizeTaskReturningThunk);
                }
            }

            return new()
            {
                ExecutionAndSyncBlockStore_Pop = KnownILStubReferences.GetKnownMethod(context, KnownILStubReference.ExecutionAndSyncBlockStore_Pop),
                ExecutionAndSyncBlockStore_Push = KnownILStubReferences.GetKnownMethod(context, KnownILStubReference.ExecutionAndSyncBlockStore_Push),
                AsyncCallContinuation = KnownILStubReferences.GetKnownMethod(context, KnownILStubReference.AsyncHelpers_AsyncCallContinuation),
                GetResultOrCompletedTask = getResultOrCompletedTask,
                TaskFromException = taskFromException,
                FinalizeTaskReturningThunk = finalizeTask,
                LogicalReturnType = logicalReturnType,
                ExecutionAndSyncBlockStore = KnownILStubReferences.GetKnownType(context, KnownILStubReference.ExecutionAndSyncBlockStore),
                Exception = KnownILStubReferences.GetKnownType(context, KnownILStubReference.Exception)
            };
        }

        private struct AsyncThunkReferences
        {
            public required MethodDesc IsCompletedMethod { get; init; }
            public required MethodDesc CompletionResultMethod { get; init; }
            public required MethodDesc? AsTaskOrNotifierMethod { get; init; }
            public required MethodDesc TransparentAwaitMethod { get; init; }

            public void EmitLoadTaskLocal(ILCodeStream stream, ILLocalVariable taskLocal)
            {
                bool isValueTask = AsTaskOrNotifierMethod is not null;
                if (isValueTask)
                {
                    stream.EmitLdLoca(taskLocal);
                }
                else
                {
                    stream.EmitLdLoc(taskLocal);
                }
            }

            public IEnumerable<MethodDesc> GetAllReferences()
            {
                yield return IsCompletedMethod;
                yield return CompletionResultMethod;
                if (AsTaskOrNotifierMethod is not null)
                {
                    yield return AsTaskOrNotifierMethod;
                }
                yield return TransparentAwaitMethod;
            }
        }

        private static AsyncThunkReferences GetAsyncThunkMethods(MethodDesc taskReturningMethod)
        {
            TypeSystemContext context = taskReturningMethod.Context;
            MethodDesc isCompleted;
            MethodDesc? asTaskOrNotifierMethod;
            MethodDesc completionResultMethod;

            TypeDesc taskReturningMethodReturnType = taskReturningMethod.Signature.ReturnType;

            if (taskReturningMethodReturnType.IsValueType)
            {
                if (!taskReturningMethodReturnType.HasInstantiation)
                {
                    // ValueTask (non-generic)
                    isCompleted = KnownILStubReferences.GetKnownMethod(context, KnownILStubReference.ValueTask_get_IsCompleted);
                    completionResultMethod = KnownILStubReferences.GetKnownMethod(context, KnownILStubReference.ValueTask_ThrowIfCompletedUnsuccessfully);
                    asTaskOrNotifierMethod = KnownILStubReferences.GetKnownMethod(context, KnownILStubReference.ValueTask_AsTaskOrNotifier);
                }
                else
                {
                    // ValueTask<T> (generic)
                    isCompleted = KnownILStubReferences.GetKnownMethod(context, KnownILStubReference.ValueTask_1_get_IsCompleted);
                    completionResultMethod = KnownILStubReferences.GetKnownMethod(context, KnownILStubReference.ValueTask_1_get_Result);
                    asTaskOrNotifierMethod = KnownILStubReferences.GetKnownMethod(context, KnownILStubReference.ValueTask_1_AsTaskOrNotifier);
                }
            }
            else
            {
                asTaskOrNotifierMethod = null;
                // Task path
                isCompleted = KnownILStubReferences.GetKnownMethod(context, KnownILStubReference.Task_get_IsCompleted);
                if (!taskReturningMethodReturnType.HasInstantiation)
                {
                    // Task (non-generic)
                    completionResultMethod = KnownILStubReferences.GetKnownMethod(context, KnownILStubReference.AsyncHelpers_CompletedTask);
                }
                else
                {
                    // Task<T> (generic)
                    TypeDesc logicalReturnType = taskReturningMethodReturnType.Instantiation[0];
                    MethodDesc completedTaskResultMethodOpen = KnownILStubReferences.GetKnownMethod(context, KnownILStubReference.AsyncHelpers_CompletedTaskResult);
                    completionResultMethod = completedTaskResultMethodOpen.MakeInstantiatedMethod(new Instantiation(logicalReturnType));
                }
            }

            return new()
            {
                IsCompletedMethod = isCompleted,
                AsTaskOrNotifierMethod = asTaskOrNotifierMethod,
                CompletionResultMethod = completionResultMethod,
                TransparentAwaitMethod = KnownILStubReferences.GetKnownMethod(context, KnownILStubReference.AsyncHelpers_TransparentAwait)
            };
        }

        /// <summary>
        /// Get the list of methods required by the task returning thunk of an async method. These methods may not have MethodRefs in the original module, so they may need to be added to the MutableModule.
        /// </summary>
        public static IEnumerable<TypeSystemEntity> GetRequiredReferencesForTaskReturningThunk(MethodDesc taskReturningMethod)
        {
            Debug.Assert(!taskReturningMethod.IsAsyncVariant());
            var thunkMethods = GetTaskReturningThunkMethods(taskReturningMethod);
            return thunkMethods.GetAllReferences();
        }

        /// <summary>
        /// Get the list of methods required by the task returning thunk of an async method. These methods may not have MethodRefs in the original module, so they may need to be added to the MutableModule.
        /// </summary>
        // This method should match the methods used in EmitAsyncMethodThunk
        public static IEnumerable<TypeSystemEntity> GetRequiredReferencesForAsyncThunk(MethodDesc taskReturningMethod)
        {
            Debug.Assert(!taskReturningMethod.IsAsyncVariant());
            var thunkMethods = GetAsyncThunkMethods(taskReturningMethod);
            return thunkMethods.GetAllReferences();
        }

        // Emits a thunk that wraps an async method to return a Task or ValueTask.
        // The thunk calls the async method, and if it completes synchronously,
        // it returns a completed Task/ValueTask. If the async method suspends,
        // it calls FinalizeTaskReturningThunk/FinalizeValueTaskReturningThunk method to get the Task/ValueTask.

        // The emitted code matches method EmitTaskReturningThunk in CoreCLR VM.
        // Any new methods added to the emitted code should also be added to GetRequiredReferencesForTaskReturningThunk.
        public static MethodIL EmitTaskReturningThunk(MethodDesc taskReturningMethod, MethodDesc asyncMethod)
        {
            TypeSystemContext context = taskReturningMethod.Context;

            var emitter = new ILEmitter();
            var codestream = emitter.NewCodeStream();

            var thunkHelpers = GetTaskReturningThunkMethods(taskReturningMethod);

            MethodSignature sig = taskReturningMethod.Signature;
            TypeDesc returnType = sig.ReturnType;
            TypeDesc logicalReturnType = thunkHelpers.LogicalReturnType;
            ILLocalVariable logicalResultLocal = 0;
            if (!logicalReturnType.IsVoid)
            {
                logicalResultLocal = emitter.NewLocal(logicalReturnType);
            }

            ILLocalVariable returnTaskLocal = emitter.NewLocal(returnType);

            TypeDesc executionAndSyncBlockStoreType = thunkHelpers.ExecutionAndSyncBlockStore;
            ILLocalVariable executionAndSyncBlockStoreLocal = emitter.NewLocal(executionAndSyncBlockStoreType);

            ILCodeLabel returnTaskLabel = emitter.NewCodeLabel();
            ILCodeLabel suspendedLabel = emitter.NewCodeLabel();
            ILCodeLabel finishedLabel = emitter.NewCodeLabel();

            codestream.EmitLdLoca(executionAndSyncBlockStoreLocal);
            codestream.Emit(ILOpcode.call, emitter.NewToken(thunkHelpers.ExecutionAndSyncBlockStore_Push));

            ILExceptionRegionBuilder tryFinallyRegion = emitter.NewFinallyRegion();
            {
                codestream.BeginTry(tryFinallyRegion);
                codestream.Emit(ILOpcode.nop);

                TypeDesc exceptionType = thunkHelpers.Exception;
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

                    asyncMethod = InstantiateAsOpen(asyncMethod);
                    codestream.Emit(ILOpcode.call, emitter.NewToken(asyncMethod));

                    if (!logicalReturnType.IsVoid)
                    {
                        codestream.EmitStLoc(logicalResultLocal);
                    }

                    codestream.Emit(ILOpcode.call, emitter.NewToken(thunkHelpers.AsyncCallContinuation));
                    codestream.Emit(ILOpcode.brfalse, finishedLabel);
                    codestream.Emit(ILOpcode.leave, suspendedLabel);
                    codestream.EmitLabel(finishedLabel);

                    if (!logicalReturnType.IsVoid)
                    {
                        codestream.EmitLdLoc(logicalResultLocal);
                    }

                    codestream.Emit(ILOpcode.call, emitter.NewToken(thunkHelpers.GetResultOrCompletedTask));
                    codestream.EmitStLoc(returnTaskLocal);
                    codestream.Emit(ILOpcode.leave, returnTaskLabel);

                    codestream.EndTry(tryCatchRegion);
                }
                // Catch
                {
                    codestream.BeginHandler(tryCatchRegion);
                    codestream.Emit(ILOpcode.call, emitter.NewToken(thunkHelpers.TaskFromException));
                    codestream.EmitStLoc(returnTaskLocal);
                    codestream.Emit(ILOpcode.leave, returnTaskLabel);

                    codestream.EndHandler(tryCatchRegion);
                }

                codestream.EmitLabel(suspendedLabel);
                codestream.Emit(ILOpcode.call, emitter.NewToken(thunkHelpers.FinalizeTaskReturningThunk));
                codestream.EmitStLoc(returnTaskLocal);
                codestream.Emit(ILOpcode.leave, returnTaskLabel);

                codestream.EndTry(tryFinallyRegion);
            }

            {
                codestream.BeginHandler(tryFinallyRegion);
                codestream.EmitLdLoca(executionAndSyncBlockStoreLocal);
                codestream.Emit(ILOpcode.call, emitter.NewToken(KnownILStubReferences.GetKnownMethod(context, KnownILStubReference.ExecutionAndSyncBlockStore_Pop)));
                codestream.Emit(ILOpcode.endfinally);
                codestream.EndHandler(tryFinallyRegion);
            }

            codestream.EmitLabel(returnTaskLabel);
            codestream.EmitLdLoc(returnTaskLocal);
            codestream.Emit(ILOpcode.ret);

            return emitter.Link(taskReturningMethod);
        }

        // The emitted code matches method EmitAsyncMethodThunk in CoreCLR VM.
        // Any new methods added to the emitted code should also be added to GetRequiredReferencesForAsyncThunk.
        public static MethodIL EmitAsyncMethodThunk(MethodDesc asyncMethod, MethodDesc taskReturningMethod)
        {
            var emitter = new ILEmitter();
            var codestream = emitter.NewCodeStream();

            taskReturningMethod = InstantiateAsOpen(taskReturningMethod);
            var thunkHelpers = GetAsyncThunkMethods(taskReturningMethod);

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

            ILLocalVariable taskLocal = emitter.NewLocal(taskReturningMethod.Signature.ReturnType);
            ILCodeLabel taskCompletedLabel = emitter.NewCodeLabel();

            codestream.EmitStLoc(taskLocal);
            thunkHelpers.EmitLoadTaskLocal(codestream, taskLocal);
            codestream.Emit(ILOpcode.call, emitter.NewToken(thunkHelpers.IsCompletedMethod));
            codestream.Emit(ILOpcode.brtrue, taskCompletedLabel);

            thunkHelpers.EmitLoadTaskLocal(codestream, taskLocal);
            if (thunkHelpers.AsTaskOrNotifierMethod is not null)
            {
                codestream.Emit(ILOpcode.call, emitter.NewToken(thunkHelpers.AsTaskOrNotifierMethod));
            }
            codestream.Emit(ILOpcode.call, emitter.NewToken(thunkHelpers.TransparentAwaitMethod));
            codestream.EmitLabel(taskCompletedLabel);
            thunkHelpers.EmitLoadTaskLocal(codestream, taskLocal);
            codestream.Emit(ILOpcode.call, emitter.NewToken(thunkHelpers.CompletionResultMethod));
            codestream.Emit(ILOpcode.ret);

            return emitter.Link(asyncMethod);
        }

        private static MethodDesc InstantiateAsOpen(MethodDesc method)
        {
            var context = method.Context;
            if (method.OwningType.HasInstantiation)
            {
                var instantiatedType = (InstantiatedType)TypeSystemHelpers.InstantiateAsOpen((TypeDesc)method.OwningType);
                method = context.GetMethodForInstantiatedType(method, instantiatedType);
            }

            if (method.HasInstantiation)
            {
                var inst = new TypeDesc[method.Instantiation.Length];
                for (int i = 0; i < inst.Length; i++)
                {
                    inst[i] = context.GetSignatureVariable(i, true);
                }
                method = method.MakeInstantiatedMethod(new Instantiation(inst));
            }

            return method;
        }
    }
}

#nullable restore
