// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Internal.TypeSystem;
using System.Collections.Generic;
using System.Diagnostics;
using Internal.TypeSystem.Ecma;
using ILCompiler;

#nullable enable

namespace Internal.IL.Stubs
{
    public static class AsyncThunkILEmitter
    {
        private enum AsyncHelper
        {
            ExecutionAndSyncBlockStore,
            ExecutionAndSyncBlockStore_Push,
            ExecutionAndSyncBlockStore_Pop,

            AsyncHelpers,

            AsyncHelpers_AsyncCallContinuation,
            AsyncHelpers_TransparentAwait,
            AsyncHelpers_CompletedTask,
            AsyncHelpers_CompletedTaskResult,

            AsyncHelpers_TaskFromException,
            AsyncHelpers_TaskFromException_1,
            AsyncHelpers_ValueTaskFromException,
            AsyncHelpers_ValueTaskFromException_1,

            AsyncHelpers_FinalizeTaskReturningThunk,
            AsyncHelpers_FinalizeTaskReturningThunk_1,
            AsyncHelpers_FinalizeValueTaskReturningThunk,
            AsyncHelpers_FinalizeValueTaskReturningThunk_1,

            Task,
            Task_1,
            ValueTask,
            ValueTask_1,

            Task_FromResult,
            ValueTask_FromResult_1,

            Task_get_CompletedTask,
            ValueTask_get_CompletedTask,

            Task_get_IsCompleted,
            Task_1_get_IsCompleted,
            ValueTask_get_IsCompleted,
            ValueTask_1_get_IsCompleted,

            ValueTask_get_Result,
            ValueTask_1_get_Result,

            ValueTask_AsTaskOrNotifier,
            ValueTask_1_AsTaskOrNotifier,

            AsyncHelperCount,
        }

        [Conditional("DEBUG")]
        public static void AssertIsKnownAsyncHelper(TypeSystemEntity entity, string message)
        {
            TypeSystemContext context = entity.Context;
            entity = entity switch
            {
                MethodDesc m => m.GetTypicalMethodDefinition(),
                TypeDesc t => t.GetTypeDefinition(),
                _ => throw new ArgumentException("entity must be MethodDesc or TypeDesc")
            };
            for (AsyncHelper helper = 0; helper < AsyncHelper.AsyncHelperCount; helper++)
            {
                MethodDesc knownHelperMethod = GetHelperMethod(context, helper);
                if (entity == knownHelperMethod)
                {
                    return;
                }
            }
            throw new InvalidOperationException(message);
        }

        private static EcmaMethod GetHelperMethod(TypeSystemContext context, AsyncHelper helper)
        {
            return (EcmaMethod)(helper switch
            {
                AsyncHelper.ExecutionAndSyncBlockStore => throw new ArgumentException("Use a more specific enum value"),
                AsyncHelper.ExecutionAndSyncBlockStore_Push => GetHelperType(context, AsyncHelper.ExecutionAndSyncBlockStore).GetKnownMethod("Push"u8, null),
                AsyncHelper.ExecutionAndSyncBlockStore_Pop => GetHelperType(context, AsyncHelper.ExecutionAndSyncBlockStore).GetKnownMethod("Pop"u8, null),

                AsyncHelper.AsyncHelpers_AsyncCallContinuation => GetHelperType(context, AsyncHelper.AsyncHelpers).GetKnownMethod("AsyncCallContinuation"u8, null),
                AsyncHelper.AsyncHelpers_TransparentAwait => GetHelperType(context, AsyncHelper.AsyncHelpers).GetKnownMethod("TransparentAwait"u8, null),
                AsyncHelper.AsyncHelpers_CompletedTask => GetHelperType(context, AsyncHelper.AsyncHelpers).GetKnownMethod("CompletedTask"u8, null),
                AsyncHelper.AsyncHelpers_CompletedTaskResult => GetHelperType(context, AsyncHelper.AsyncHelpers).GetKnownMethod("CompletedTaskResult"u8, null),

                AsyncHelper.AsyncHelpers_TaskFromException => GetHelperType(context, AsyncHelper.AsyncHelpers).GetKnownMethod("TaskFromException"u8, new MethodSignature(MethodSignatureFlags.Static, 0, Task(), [Exception()])),
                AsyncHelper.AsyncHelpers_TaskFromException_1 => GetHelperType(context, AsyncHelper.AsyncHelpers).GetKnownMethod("TaskFromException"u8, new MethodSignature(MethodSignatureFlags.Static, 1, Task_T(), [Exception()])),
                AsyncHelper.AsyncHelpers_ValueTaskFromException => GetHelperType(context, AsyncHelper.AsyncHelpers).GetKnownMethod("ValueTaskFromException"u8, new MethodSignature(MethodSignatureFlags.Static, 0, ValueTask(), [Exception()])),
                AsyncHelper.AsyncHelpers_ValueTaskFromException_1 => GetHelperType(context, AsyncHelper.AsyncHelpers).GetKnownMethod("ValueTaskFromException"u8, new MethodSignature(MethodSignatureFlags.Static, 1, ValueTask_T(), [Exception()])),

                AsyncHelper.AsyncHelpers_FinalizeTaskReturningThunk => GetHelperType(context, AsyncHelper.AsyncHelpers).GetKnownMethod("FinalizeTaskReturningThunk"u8, new MethodSignature(MethodSignatureFlags.Static, 0, Task(), [])),
                AsyncHelper.AsyncHelpers_FinalizeTaskReturningThunk_1 => GetHelperType(context, AsyncHelper.AsyncHelpers).GetKnownMethod("FinalizeTaskReturningThunk"u8, new MethodSignature(MethodSignatureFlags.Static, 1, Task_T(), [])),
                AsyncHelper.AsyncHelpers_FinalizeValueTaskReturningThunk => GetHelperType(context, AsyncHelper.AsyncHelpers).GetKnownMethod("FinalizeValueTaskReturningThunk"u8, new MethodSignature(MethodSignatureFlags.Static, 0, ValueTask(), [])),
                AsyncHelper.AsyncHelpers_FinalizeValueTaskReturningThunk_1 => GetHelperType(context, AsyncHelper.AsyncHelpers).GetKnownMethod("FinalizeValueTaskReturningThunk"u8, new MethodSignature(MethodSignatureFlags.Static, 1, ValueTask_T(), [])),

                AsyncHelper.Task_FromResult => GetHelperType(context, AsyncHelper.Task).GetKnownMethod("FromResult"u8, null),
                AsyncHelper.ValueTask_FromResult_1 => GetHelperType(context, AsyncHelper.ValueTask_1).GetKnownMethod("FromResult"u8, null),

                AsyncHelper.Task_get_CompletedTask => GetHelperType(context, AsyncHelper.Task).GetKnownMethod("get_CompletedTask"u8, null),
                AsyncHelper.ValueTask_get_CompletedTask => GetHelperType(context, AsyncHelper.ValueTask).GetKnownMethod("get_CompletedTask"u8, null),

                AsyncHelper.Task_get_IsCompleted => GetHelperType(context, AsyncHelper.Task).GetKnownMethod("get_IsCompleted"u8, null),
                AsyncHelper.Task_1_get_IsCompleted => GetHelperType(context, AsyncHelper.Task_1).GetKnownMethod("get_IsCompleted"u8, null),
                AsyncHelper.ValueTask_get_IsCompleted => GetHelperType(context, AsyncHelper.ValueTask).GetKnownMethod("get_IsCompleted"u8, null),
                AsyncHelper.ValueTask_1_get_IsCompleted => GetHelperType(context, AsyncHelper.ValueTask_1).GetKnownMethod("get_IsCompleted"u8, null),

                AsyncHelper.ValueTask_get_Result => GetHelperType(context, AsyncHelper.ValueTask).GetKnownMethod("get_Result"u8, null),
                AsyncHelper.ValueTask_1_get_Result => GetHelperType(context, AsyncHelper.ValueTask_1).GetKnownMethod("get_Result"u8, null),

                AsyncHelper.ValueTask_AsTaskOrNotifier => GetHelperType(context, AsyncHelper.ValueTask).GetKnownMethod("AsTaskOrNotifier"u8, null),
                AsyncHelper.ValueTask_1_AsTaskOrNotifier => GetHelperType(context, AsyncHelper.ValueTask_1).GetKnownMethod("AsTaskOrNotifier"u8, null),

                _ => throw new ArgumentOutOfRangeException(nameof(helper))
            });

            TypeDesc Task() => GetHelperType(context, AsyncHelper.Task);
            TypeDesc Task_T() => GetHelperType(context, AsyncHelper.Task_1).MakeInstantiatedType(context.GetSignatureVariable(0, true));
            TypeDesc ValueTask() => GetHelperType(context, AsyncHelper.ValueTask);
            TypeDesc ValueTask_T() => GetHelperType(context, AsyncHelper.ValueTask_1).MakeInstantiatedType(context.GetSignatureVariable(0, true));
            TypeDesc Exception() => context.GetWellKnownType(WellKnownType.Exception);
        }

        private static EcmaType GetHelperType(TypeSystemContext context, AsyncHelper helper)
        {
            return (EcmaType)(helper switch
            {
                AsyncHelper.ExecutionAndSyncBlockStore => context.SystemModule.GetKnownType("System.Runtime.CompilerServices"u8, "ExecutionAndSyncBlockStore"u8),
                AsyncHelper.AsyncHelpers => context.SystemModule.GetKnownType("System.Runtime.CompilerServices"u8, "AsyncHelpers"u8),
                AsyncHelper.Task => context.SystemModule.GetKnownType("System.Threading.Tasks"u8, "Task"u8),
                AsyncHelper.Task_1 => context.SystemModule.GetKnownType("System.Threading.Tasks"u8, "Task`1"u8),
                AsyncHelper.ValueTask => context.SystemModule.GetKnownType("System.Threading.Tasks"u8, "ValueTask"u8),
                AsyncHelper.ValueTask_1 => context.SystemModule.GetKnownType("System.Threading.Tasks"u8, "ValueTask`1"u8),
                _ => throw new ArgumentOutOfRangeException(nameof(helper))
            });
        }

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

        private static TaskReturningThunkReferences  GetTaskReturningThunkMethods(MethodDesc taskReturningMethod)
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
                    getResultOrCompletedTask = GetHelperMethod(context, AsyncHelper.ValueTask_FromResult_1)
                        .MakeInstantiatedMethod(new Instantiation(logicalReturnType));
                    taskFromException = GetHelperMethod(context, AsyncHelper.AsyncHelpers_ValueTaskFromException_1)
                        .MakeInstantiatedMethod(new Instantiation(logicalReturnType));
                    finalizeTask = GetHelperMethod(context, AsyncHelper.AsyncHelpers_FinalizeValueTaskReturningThunk_1)
                        .MakeInstantiatedMethod(new Instantiation(logicalReturnType));
                }
                else
                {
                    getResultOrCompletedTask = GetHelperMethod(context, AsyncHelper.Task_FromResult)
                        .MakeInstantiatedMethod(new Instantiation(logicalReturnType));
                    taskFromException = GetHelperMethod(context, AsyncHelper.AsyncHelpers_TaskFromException_1)
                        .MakeInstantiatedMethod(new Instantiation(logicalReturnType));
                    finalizeTask = GetHelperMethod(context, AsyncHelper.AsyncHelpers_FinalizeTaskReturningThunk_1)
                        .MakeInstantiatedMethod(new Instantiation(logicalReturnType));
                }
            }
            else
            {
                if (isValueTask)
                {
                    getResultOrCompletedTask = GetHelperMethod(context, AsyncHelper.ValueTask_get_CompletedTask);
                    taskFromException = GetHelperMethod(context, AsyncHelper.AsyncHelpers_ValueTaskFromException);
                    finalizeTask = GetHelperMethod(context, AsyncHelper.AsyncHelpers_FinalizeValueTaskReturningThunk);
                }
                else
                {
                    getResultOrCompletedTask = GetHelperMethod(context, AsyncHelper.Task_get_CompletedTask);
                    taskFromException = GetHelperMethod(context, AsyncHelper.AsyncHelpers_TaskFromException);
                    finalizeTask = GetHelperMethod(context, AsyncHelper.AsyncHelpers_FinalizeTaskReturningThunk);
                }
            }

            return new()
            {
                ExecutionAndSyncBlockStore_Pop = GetHelperMethod(context, AsyncHelper.ExecutionAndSyncBlockStore_Pop),
                ExecutionAndSyncBlockStore_Push = GetHelperMethod(context, AsyncHelper.ExecutionAndSyncBlockStore_Push),
                AsyncCallContinuation = GetHelperMethod(context, AsyncHelper.AsyncHelpers_AsyncCallContinuation),
                GetResultOrCompletedTask = getResultOrCompletedTask,
                TaskFromException = taskFromException,
                FinalizeTaskReturningThunk = finalizeTask,
                LogicalReturnType = logicalReturnType,
                ExecutionAndSyncBlockStore = GetHelperType(context, AsyncHelper.ExecutionAndSyncBlockStore),
                Exception = context.GetWellKnownType(WellKnownType.Exception)
            };
        }

        private struct AsyncThunkReferences
        {
            public required MethodDesc IsCompletedMethod { get; init; }
            public required MethodDesc CompletionResultMethod { get; init; }
            public required MethodDesc? AsTaskOrNotifierMethod { get; init; }
            public required MethodDesc TransparentAwaitMethod { get; init; }

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
                    isCompleted = GetHelperMethod(context, AsyncHelper.ValueTask_get_IsCompleted);
                    completionResultMethod = GetHelperMethod(context, AsyncHelper.ValueTask_get_Result);
                    asTaskOrNotifierMethod = GetHelperMethod(context, AsyncHelper.ValueTask_AsTaskOrNotifier);
                }
                else
                {
                    // ValueTask<T> (generic)
                    isCompleted = GetHelperMethod(context, AsyncHelper.ValueTask_1_get_IsCompleted);
                    completionResultMethod = GetHelperMethod(context, AsyncHelper.ValueTask_1_get_Result);
                    asTaskOrNotifierMethod = GetHelperMethod(context, AsyncHelper.ValueTask_1_AsTaskOrNotifier);
                }
            }
            else
            {
                asTaskOrNotifierMethod = null;
                // Task path
                if (!taskReturningMethodReturnType.HasInstantiation)
                {
                    // Task (non-generic)
                    isCompleted = GetHelperMethod(context, AsyncHelper.Task_get_IsCompleted);
                    completionResultMethod = GetHelperMethod(context, AsyncHelper.AsyncHelpers_CompletedTask);
                }
                else
                {
                    // Task<T> (generic)
                    isCompleted = GetHelperMethod(context, AsyncHelper.Task_1_get_IsCompleted);
                    TypeDesc logicalReturnType = taskReturningMethodReturnType.Instantiation[0];
                    MethodDesc completedTaskResultMethodOpen = GetHelperMethod(context, AsyncHelper.AsyncHelpers_CompletedTaskResult);
                    completionResultMethod = completedTaskResultMethodOpen.MakeInstantiatedMethod(new Instantiation(logicalReturnType));
                }
            }

            return new()
            {
                IsCompletedMethod = isCompleted,
                AsTaskOrNotifierMethod = asTaskOrNotifierMethod,
                CompletionResultMethod = completionResultMethod,
                TransparentAwaitMethod = GetHelperMethod(context, AsyncHelper.AsyncHelpers_TransparentAwait)
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
            codestream.Emit(ILOpcode.call, emitter.NewToken(thunkHelpers.ExecutionAndSyncBlockStore_Pop));

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
                codestream.Emit(ILOpcode.call, emitter.NewToken(GetHelperMethod(context, AsyncHelper.ExecutionAndSyncBlockStore_Pop)));
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
            codestream.EmitLdLoca(taskLocal);
            codestream.Emit(ILOpcode.call, emitter.NewToken(thunkHelpers.IsCompletedMethod));
            codestream.Emit(ILOpcode.brtrue, taskCompletedLabel);

            codestream.EmitLdLoca(taskLocal);
            if (thunkHelpers.AsTaskOrNotifierMethod is not null)
            {
                codestream.Emit(ILOpcode.call, emitter.NewToken(thunkHelpers.AsTaskOrNotifierMethod));
            }
            codestream.Emit(ILOpcode.call, emitter.NewToken(thunkHelpers.TransparentAwaitMethod));
            codestream.EmitLabel(taskCompletedLabel);
            codestream.EmitLdLoca(taskLocal);
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
