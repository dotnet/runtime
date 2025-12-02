// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Internal.TypeSystem;
using Internal.ReadyToRunConstants;
using System.Collections.Generic;

#nullable enable

namespace Internal.IL.Stubs
{
    public static class AsyncThunkILEmitter
    {
        private enum AsyncHelper
        {
            ExecutionAndSyncBlockStore_Push,
            ExecutionAndSyncBlockStore_Pop,
            AsyncCallContinuation,
            TransparentAwait,

            Task_FromResult,
            ValueTask_FromResult_1,

            Task_get_CompletedTask,
            ValueTask_get_CompletedTask,

            TaskFromException,
            TaskFromException_1,
            ValueTaskFromException,
            ValueTaskFromException_1,

            FinalizeTaskReturningThunk,
            FinalizeTaskReturningThunk_1,
            FinalizeValueTaskReturningThunk,
            FinalizeValueTaskReturningThunk_1,

            Task_get_IsCompleted,
            Task_1_get_IsCompleted,
            ValueTask_get_IsCompleted,
            ValueTask_1_get_IsCompleted,

            ValueTask_get_Result,
            ValueTask_1_get_Result,

            ValueTask_AsTaskOrNotifier,
            ValueTask_1_AsTaskOrNotifier,

            CompletedTask,
            CompletedTaskResult,

            AsyncHelperCount,
        }

        private static MethodDesc GetHelperMethod(TypeSystemContext context, AsyncHelper helper)
        {
            return helper switch
            {
                AsyncHelper.ExecutionAndSyncBlockStore_Push => context.SystemModule.GetKnownType("System.Runtime.CompilerServices"u8, "ExecutionAndSyncBlockStore"u8).GetKnownMethod("Push"u8, null),
                AsyncHelper.ExecutionAndSyncBlockStore_Pop => context.SystemModule.GetKnownType("System.Runtime.CompilerServices"u8, "ExecutionAndSyncBlockStore"u8).GetKnownMethod("Pop"u8, null),
                AsyncHelper.AsyncCallContinuation => context.SystemModule.GetKnownType("System.Runtime.CompilerServices"u8, "AsyncHelpers"u8).GetKnownMethod("AsyncCallContinuation"u8, null),
                AsyncHelper.TransparentAwait => context.SystemModule.GetKnownType("System.Runtime.CompilerServices"u8, "AsyncHelpers"u8).GetKnownMethod("TransparentAwait"u8, null),

                AsyncHelper.Task_FromResult => context.SystemModule.GetKnownType("System.Threading.Tasks"u8, "Task"u8).GetKnownMethod("FromResult"u8, null),
                AsyncHelper.ValueTask_FromResult_1 => context.SystemModule.GetKnownType("System.Threading.Tasks"u8, "ValueTask"u8).GetKnownMethod("FromResult"u8, null),

                AsyncHelper.Task_get_CompletedTask => context.SystemModule.GetKnownType("System.Threading.Tasks"u8, "Task"u8).GetKnownMethod("get_CompletedTask"u8, null),
                AsyncHelper.ValueTask_get_CompletedTask => context.SystemModule.GetKnownType("System.Threading.Tasks"u8, "ValueTask"u8).GetKnownMethod("get_CompletedTask"u8, null),

                AsyncHelper.TaskFromException => context.SystemModule.GetKnownType("System.Runtime.CompilerServices"u8, "AsyncHelpers"u8).GetKnownMethod("TaskFromException"u8, new MethodSignature(MethodSignatureFlags.Static, 0, Task(), [Exception()])),
                AsyncHelper.TaskFromException_1 => context.SystemModule.GetKnownType("System.Runtime.CompilerServices"u8, "AsyncHelpers"u8).GetKnownMethod("TaskFromException"u8, new MethodSignature(MethodSignatureFlags.Static, 1, Task_T(), [Exception()])),
                AsyncHelper.ValueTaskFromException => context.SystemModule.GetKnownType("System.Runtime.CompilerServices"u8, "AsyncHelpers"u8).GetKnownMethod("ValueTaskFromException"u8, new MethodSignature(MethodSignatureFlags.Static, 0, ValueTask(), [Exception()])),
                AsyncHelper.ValueTaskFromException_1 => context.SystemModule.GetKnownType("System.Runtime.CompilerServices"u8, "AsyncHelpers"u8).GetKnownMethod("ValueTaskFromException"u8, new MethodSignature(MethodSignatureFlags.Static, 1, ValueTask_T(), [Exception()])),

                AsyncHelper.FinalizeTaskReturningThunk => context.SystemModule.GetKnownType("System.Runtime.CompilerServices"u8, "AsyncHelpers"u8).GetKnownMethod("FinalizeTaskReturningThunk"u8, new MethodSignature(MethodSignatureFlags.Static, 0, Task(), [])),
                AsyncHelper.FinalizeTaskReturningThunk_1 => context.SystemModule.GetKnownType("System.Runtime.CompilerServices"u8, "AsyncHelpers"u8).GetKnownMethod("FinalizeTaskReturningThunk"u8, new MethodSignature(MethodSignatureFlags.Static, 1, Task_T(), [])),
                AsyncHelper.FinalizeValueTaskReturningThunk => context.SystemModule.GetKnownType("System.Runtime.CompilerServices"u8, "AsyncHelpers"u8).GetKnownMethod("FinalizeValueTaskReturningThunk"u8, new MethodSignature(MethodSignatureFlags.Static, 0, ValueTask(), [])),
                AsyncHelper.FinalizeValueTaskReturningThunk_1 => context.SystemModule.GetKnownType("System.Runtime.CompilerServices"u8, "AsyncHelpers"u8).GetKnownMethod("FinalizeValueTaskReturningThunk"u8, new MethodSignature(MethodSignatureFlags.Static, 1, ValueTask_T(), [])),

                AsyncHelper.Task_get_IsCompleted => context.SystemModule.GetKnownType("System.Threading.Tasks"u8, "Task"u8).GetKnownMethod("get_IsCompleted"u8, null),
                AsyncHelper.Task_1_get_IsCompleted => context.SystemModule.GetKnownType("System.Threading.Tasks"u8, "Task`1"u8).GetKnownMethod("get_IsCompleted"u8, null),
                AsyncHelper.ValueTask_get_IsCompleted => context.SystemModule.GetKnownType("System.Threading.Tasks"u8, "ValueTask"u8).GetKnownMethod("get_IsCompleted"u8, null),
                AsyncHelper.ValueTask_1_get_IsCompleted => context.SystemModule.GetKnownType("System.Threading.Tasks"u8, "ValueTask`1"u8).GetKnownMethod("get_IsCompleted"u8, null),

                AsyncHelper.ValueTask_get_Result => context.SystemModule.GetKnownType("System.Threading.Tasks"u8, "ValueTask"u8).GetKnownMethod("get_Result"u8, null),
                AsyncHelper.ValueTask_1_get_Result => context.SystemModule.GetKnownType("System.Threading.Tasks"u8, "ValueTask`1"u8).GetKnownMethod("get_Result"u8, null),

                AsyncHelper.ValueTask_AsTaskOrNotifier => context.SystemModule.GetKnownType("System.Threading.Tasks"u8, "ValueTask"u8).GetKnownMethod("AsTaskOrNotifier"u8, null),
                AsyncHelper.ValueTask_1_AsTaskOrNotifier => context.SystemModule.GetKnownType("System.Threading.Tasks"u8, "ValueTask`1"u8).GetKnownMethod("AsTaskOrNotifier"u8, null),

                AsyncHelper.CompletedTask => context.SystemModule.GetKnownType("System.Threading.Tasks"u8, "Task"u8).GetKnownMethod("CompletedTask"u8, null),
                AsyncHelper.CompletedTaskResult => context.SystemModule.GetKnownType("System.Threading.Tasks"u8, "Task"u8).GetKnownMethod("CompletedTaskResult"u8, null),

                _ => throw new ArgumentOutOfRangeException(nameof(helper))
            };

            TypeDesc Task() => context.SystemModule.GetKnownType("System.Threading.Tasks"u8, "Task"u8);
            TypeDesc Task_T() => context.SystemModule.GetKnownType("System.Threading.Tasks"u8, "Task`1"u8).MakeInstantiatedType(context.GetSignatureVariable(0, true));
            TypeDesc ValueTask() => context.SystemModule.GetKnownType("System.Threading.Tasks"u8, "ValueTask"u8);
            TypeDesc ValueTask_T() => context.SystemModule.GetKnownType("System.Threading.Tasks"u8, "ValueTask`1"u8).MakeInstantiatedType(context.GetSignatureVariable(0, true));
            TypeDesc Exception() => context.GetWellKnownType(WellKnownType.Exception);
        }

        private static MethodDesc GetHelperMethod(TypeSystemContext context, ReadyToRunHelper helper)
        {
            ILCompiler.JitHelper.GetEntryPoint(context, helper, out _, out MethodDesc methodDesc);
            return methodDesc;
        }

        public struct TaskReturningThunkMethods
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

            public IEnumerable<MethodDesc> GetMethods()
            {
                yield return ExecutionAndSyncBlockStore_Push;
                yield return AsyncCallContinuation;
                yield return GetResultOrCompletedTask;
                yield return TaskFromException;
                yield return FinalizeTaskReturningThunk;
                yield return ExecutionAndSyncBlockStore_Pop;
            }

            public IEnumerable<TypeDesc> GetTypes()
            {
                yield return LogicalReturnType;
                yield return ExecutionAndSyncBlockStore;
            }
        }

        private static void GetTaskReturningThunkMethods(MethodDesc taskReturningMethod, MethodDesc _, out TaskReturningThunkMethods thunkMethods)
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
                    taskFromException = GetHelperMethod(context, AsyncHelper.ValueTaskFromException_1)
                        .MakeInstantiatedMethod(new Instantiation(logicalReturnType));
                    finalizeTask = GetHelperMethod(context, AsyncHelper.FinalizeValueTaskReturningThunk_1)
                        .MakeInstantiatedMethod(new Instantiation(logicalReturnType));
                }
                else
                {
                    getResultOrCompletedTask = GetHelperMethod(context, AsyncHelper.Task_FromResult)
                        .MakeInstantiatedMethod(new Instantiation(logicalReturnType));
                    taskFromException = GetHelperMethod(context, AsyncHelper.TaskFromException_1)
                        .MakeInstantiatedMethod(new Instantiation(logicalReturnType));
                    finalizeTask = GetHelperMethod(context, AsyncHelper.FinalizeTaskReturningThunk_1)
                        .MakeInstantiatedMethod(new Instantiation(logicalReturnType));
                }
            }
            else
            {
                if (isValueTask)
                {
                    getResultOrCompletedTask = GetHelperMethod(context, AsyncHelper.ValueTask_get_CompletedTask);
                    taskFromException = GetHelperMethod(context, AsyncHelper.ValueTaskFromException);
                    finalizeTask = GetHelperMethod(context, AsyncHelper.FinalizeValueTaskReturningThunk);
                }
                else
                {
                    getResultOrCompletedTask = GetHelperMethod(context, AsyncHelper.Task_get_CompletedTask);
                    taskFromException = GetHelperMethod(context, AsyncHelper.TaskFromException);
                    finalizeTask = GetHelperMethod(context, AsyncHelper.FinalizeTaskReturningThunk);
                }
            }

            thunkMethods = new()
            {
                ExecutionAndSyncBlockStore_Pop = GetHelperMethod(context, AsyncHelper.ExecutionAndSyncBlockStore_Pop),
                ExecutionAndSyncBlockStore_Push = GetHelperMethod(context, AsyncHelper.ExecutionAndSyncBlockStore_Push),
                AsyncCallContinuation = GetHelperMethod(context, AsyncHelper.AsyncCallContinuation),
                GetResultOrCompletedTask = getResultOrCompletedTask,
                TaskFromException = taskFromException,
                FinalizeTaskReturningThunk = finalizeTask,
                LogicalReturnType = logicalReturnType,
                ExecutionAndSyncBlockStore = context.SystemModule.GetKnownType("System.Runtime.CompilerServices"u8, "ExecutionAndSyncBlockStore"u8),
                Exception = context.GetWellKnownType(WellKnownType.Exception)
            };
        }

        private struct AsyncThunkMethods
        {
            public required MethodDesc IsCompletedMethod { get; init; }
            public required MethodDesc CompletionResultMethod { get; init; }
            public required MethodDesc? AsTaskOrNotifierMethod { get; init; }
            public required MethodDesc TransparentAwaitMethod { get; init; }

            public IEnumerable<MethodDesc> GetMethods()
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

        private static void GetAsyncThunkMethods(MethodDesc taskReturningMethod, MethodDesc _, out AsyncThunkMethods thunkMethods)
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
                    isCompleted = GetHelperMethod(context, AsyncHelper.ValueTask_1_get_IsCompleted);
                    completionResultMethod = GetHelperMethod(context, AsyncHelper.ValueTask_1_get_Result);
                    asTaskOrNotifierMethod = GetHelperMethod(context, AsyncHelper.ValueTask_1_AsTaskOrNotifier);
                }
                else
                {
                    // ValueTask<T> (generic)
                    isCompleted = GetHelperMethod(context, AsyncHelper.ValueTask_get_IsCompleted);
                    completionResultMethod = GetHelperMethod(context, AsyncHelper.ValueTask_get_Result);
                    asTaskOrNotifierMethod = GetHelperMethod(context, AsyncHelper.ValueTask_AsTaskOrNotifier);
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
                    completionResultMethod = GetHelperMethod(context, AsyncHelper.CompletedTask);
                }
                else
                {
                    // Task<T> (generic)
                    isCompleted = GetHelperMethod(context, AsyncHelper.Task_1_get_IsCompleted);
                    TypeDesc logicalReturnType = taskReturningMethodReturnType.Instantiation[0];
                    MethodDesc completedTaskResultMethodOpen = GetHelperMethod(context, AsyncHelper.CompletedTaskResult);
                    completionResultMethod = completedTaskResultMethodOpen.MakeInstantiatedMethod(new Instantiation(logicalReturnType));
                }
            }

            thunkMethods = new()
            {
                IsCompletedMethod = isCompleted,
                AsTaskOrNotifierMethod = asTaskOrNotifierMethod,
                CompletionResultMethod = completionResultMethod,
                TransparentAwaitMethod = GetHelperMethod(context, AsyncHelper.TransparentAwait)
            };
        }

        /// <summary>
        /// Get the list of methods required by the task returning thunk of an async method. These methods may not have MethodRefs in the original module, so they may need to be added to the MutableModule.
        /// </summary>
        public static IEnumerable<MethodDesc> GetRequiredReferencesForTaskReturningThunk(MethodDesc taskReturningMethod, MethodDesc asyncMethod)
        {
            GetTaskReturningThunkMethods(taskReturningMethod, asyncMethod, out TaskReturningThunkMethods thunkMethods);
            return thunkMethods.GetMethods();
        }

        /// <summary>
        /// Get the list of methods required by the task returning thunk of an async method. These methods may not have MethodRefs in the original module, so they may need to be added to the MutableModule.
        /// </summary>
        // This method should match the methods used in EmitAsyncMethodThunk
        public static IEnumerable<MethodDesc> GetRequiredReferencesForAsyncThunk(MethodDesc taskReturningMethod, MethodDesc asyncMethod)
        {
            GetAsyncThunkMethods(taskReturningMethod, asyncMethod, out AsyncThunkMethods thunkMethods);
            return thunkMethods.GetMethods();
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

            GetTaskReturningThunkMethods(taskReturningMethod, asyncMethod, out TaskReturningThunkMethods thunkHelpers);

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
            GetAsyncThunkMethods(taskReturningMethod, asyncMethod, out AsyncThunkMethods thunkHelpers);

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
