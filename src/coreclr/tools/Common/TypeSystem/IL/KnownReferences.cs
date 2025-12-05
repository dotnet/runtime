// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Diagnostics;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace Internal.IL
{
    public enum KnownILStubReference
    {
        Exception,

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

        Task_FromResult_1,
        ValueTask_FromResult_1,

        Task_get_CompletedTask,
        ValueTask_get_CompletedTask,

        Task_get_IsCompleted,
        ValueTask_get_IsCompleted,
        ValueTask_1_get_IsCompleted,

        ValueTask_ThrowIfCompletedUnsuccessfully,
        ValueTask_1_get_Result,

        ValueTask_AsTaskOrNotifier,
        ValueTask_1_AsTaskOrNotifier,

        AsyncHelperCount,
    }

    public class KnownILStubReferences
    {
        public static bool IsKnownMethod(MethodDesc method)
        {
            for (KnownILStubReference helper = 0; helper < KnownILStubReference.AsyncHelperCount; helper++)
            {
                TypeSystemEntity knownHelper = GetKnownEntity(method.Context, helper);
                if (knownHelper is not MethodDesc knownMethod)
                {     continue; }
                if (knownMethod.GetTypicalMethodDefinition() == method)
                {
                    return true;
                }
            }
            return false;
        }

        [Conditional("DEBUG")]
        public static void AssertIsKnownEntity(TypeSystemEntity entity, string message)
        {
            TypeSystemContext context = entity.Context;
            entity = entity switch
            {
                MethodDesc m => m.GetTypicalMethodDefinition(),
                TypeDesc t => t.GetTypeDefinition(),
                _ => throw new ArgumentException("entity must be MethodDesc or TypeDesc")
            };
            for (KnownILStubReference helper = 0; helper < KnownILStubReference.AsyncHelperCount; helper++)
            {
                TypeSystemEntity knownHelper = GetKnownEntity(context, helper);
                if (entity == knownHelper)
                {
                    return;
                }
            }
            throw new InvalidOperationException(message);
        }

        public static EcmaMethod GetKnownMethod(TypeSystemContext context, KnownILStubReference helper)
        {
            TypeSystemEntity entity = GetKnownEntity(context, helper);
            if (entity is EcmaMethod method)
                return method;
            throw new ArgumentException($"'{helper}' does not refer to a method", nameof(helper));
        }

        public static EcmaType GetKnownType(TypeSystemContext context, KnownILStubReference helper)
        {
            TypeSystemEntity entity = GetKnownEntity(context, helper);
            if (entity is EcmaType type)
                return type;
            throw new ArgumentException($"'{helper}' does not refer to a type", nameof(helper));
        }

        public static TypeSystemEntity GetKnownEntity(TypeSystemContext context, KnownILStubReference helper)
        {
            return helper switch
            {
                KnownILStubReference.Exception => context.GetWellKnownType(WellKnownType.Exception),

                KnownILStubReference.ExecutionAndSyncBlockStore => context.SystemModule.GetKnownType("System.Runtime.CompilerServices"u8, "ExecutionAndSyncBlockStore"u8),
                KnownILStubReference.ExecutionAndSyncBlockStore_Push => GetKnownType(context, KnownILStubReference.ExecutionAndSyncBlockStore).GetKnownMethod("Push"u8, null),
                KnownILStubReference.ExecutionAndSyncBlockStore_Pop => GetKnownType(context, KnownILStubReference.ExecutionAndSyncBlockStore).GetKnownMethod("Pop"u8, null),

                KnownILStubReference.AsyncHelpers => context.SystemModule.GetKnownType("System.Runtime.CompilerServices"u8, "AsyncHelpers"u8),
                KnownILStubReference.AsyncHelpers_AsyncCallContinuation => GetKnownType(context, KnownILStubReference.AsyncHelpers).GetKnownMethod("AsyncCallContinuation"u8, null),
                KnownILStubReference.AsyncHelpers_TransparentAwait => GetKnownType(context, KnownILStubReference.AsyncHelpers).GetKnownMethod("TransparentAwait"u8, null),
                KnownILStubReference.AsyncHelpers_CompletedTask => GetKnownType(context, KnownILStubReference.AsyncHelpers).GetKnownMethod("CompletedTask"u8, null),
                KnownILStubReference.AsyncHelpers_CompletedTaskResult => GetKnownType(context, KnownILStubReference.AsyncHelpers).GetKnownMethod("CompletedTaskResult"u8, null),

                KnownILStubReference.AsyncHelpers_TaskFromException => GetKnownType(context, KnownILStubReference.AsyncHelpers).GetKnownMethod("TaskFromException"u8, new MethodSignature(MethodSignatureFlags.Static, 0, Task(), [Exception()])),
                KnownILStubReference.AsyncHelpers_TaskFromException_1 => GetKnownType(context, KnownILStubReference.AsyncHelpers).GetKnownMethod("TaskFromException"u8, new MethodSignature(MethodSignatureFlags.Static, 1, Task_T(), [Exception()])),
                KnownILStubReference.AsyncHelpers_ValueTaskFromException => GetKnownType(context, KnownILStubReference.AsyncHelpers).GetKnownMethod("ValueTaskFromException"u8, new MethodSignature(MethodSignatureFlags.Static, 0, ValueTask(), [Exception()])),
                KnownILStubReference.AsyncHelpers_ValueTaskFromException_1 => GetKnownType(context, KnownILStubReference.AsyncHelpers).GetKnownMethod("ValueTaskFromException"u8, new MethodSignature(MethodSignatureFlags.Static, 1, ValueTask_T(), [Exception()])),

                KnownILStubReference.AsyncHelpers_FinalizeTaskReturningThunk => GetKnownType(context, KnownILStubReference.AsyncHelpers).GetKnownMethod("FinalizeTaskReturningThunk"u8, new MethodSignature(MethodSignatureFlags.Static, 0, Task(), [])),
                KnownILStubReference.AsyncHelpers_FinalizeTaskReturningThunk_1 => GetKnownType(context, KnownILStubReference.AsyncHelpers).GetKnownMethod("FinalizeTaskReturningThunk"u8, new MethodSignature(MethodSignatureFlags.Static, 1, Task_T(), [])),
                KnownILStubReference.AsyncHelpers_FinalizeValueTaskReturningThunk => GetKnownType(context, KnownILStubReference.AsyncHelpers).GetKnownMethod("FinalizeValueTaskReturningThunk"u8, new MethodSignature(MethodSignatureFlags.Static, 0, ValueTask(), [])),
                KnownILStubReference.AsyncHelpers_FinalizeValueTaskReturningThunk_1 => GetKnownType(context, KnownILStubReference.AsyncHelpers).GetKnownMethod("FinalizeValueTaskReturningThunk"u8, new MethodSignature(MethodSignatureFlags.Static, 1, ValueTask_T(), [])),

                KnownILStubReference.Task => context.SystemModule.GetKnownType("System.Threading.Tasks"u8, "Task"u8),
                KnownILStubReference.Task_1 => context.SystemModule.GetKnownType("System.Threading.Tasks"u8, "Task`1"u8),
                KnownILStubReference.ValueTask => context.SystemModule.GetKnownType("System.Threading.Tasks"u8, "ValueTask"u8),
                KnownILStubReference.ValueTask_1 => context.SystemModule.GetKnownType("System.Threading.Tasks"u8, "ValueTask`1"u8),

                KnownILStubReference.Task_FromResult_1 => GetKnownType(context, KnownILStubReference.Task).GetKnownMethod("FromResult"u8, null),
                KnownILStubReference.ValueTask_FromResult_1 => GetKnownType(context, KnownILStubReference.ValueTask).GetKnownMethod("FromResult"u8, null),

                KnownILStubReference.Task_get_CompletedTask => GetKnownType(context, KnownILStubReference.Task).GetKnownMethod("get_CompletedTask"u8, null),
                KnownILStubReference.ValueTask_get_CompletedTask => GetKnownType(context, KnownILStubReference.ValueTask).GetKnownMethod("get_CompletedTask"u8, null),

                KnownILStubReference.Task_get_IsCompleted => GetKnownType(context, KnownILStubReference.Task).GetKnownMethod("get_IsCompleted"u8, null),
                KnownILStubReference.ValueTask_get_IsCompleted => GetKnownType(context, KnownILStubReference.ValueTask).GetKnownMethod("get_IsCompleted"u8, null),
                KnownILStubReference.ValueTask_1_get_IsCompleted => GetKnownType(context, KnownILStubReference.ValueTask_1).GetKnownMethod("get_IsCompleted"u8, null),

                KnownILStubReference.ValueTask_ThrowIfCompletedUnsuccessfully => GetKnownType(context, KnownILStubReference.ValueTask).GetKnownMethod("ThrowIfCompletedUnsuccessfully"u8, null),
                KnownILStubReference.ValueTask_1_get_Result => GetKnownType(context, KnownILStubReference.ValueTask_1).GetKnownMethod("get_Result"u8, null),

                KnownILStubReference.ValueTask_AsTaskOrNotifier => GetKnownType(context, KnownILStubReference.ValueTask).GetKnownMethod("AsTaskOrNotifier"u8, null),
                KnownILStubReference.ValueTask_1_AsTaskOrNotifier => GetKnownType(context, KnownILStubReference.ValueTask_1).GetKnownMethod("AsTaskOrNotifier"u8, null),

                _ => throw new ArgumentOutOfRangeException(nameof(helper))
            };

            TypeDesc Task() => GetKnownType(context, KnownILStubReference.Task);
            TypeDesc Task_T() => GetKnownType(context, KnownILStubReference.Task_1).MakeInstantiatedType(context.GetSignatureVariable(0, true));
            TypeDesc ValueTask() => GetKnownType(context, KnownILStubReference.ValueTask);
            TypeDesc ValueTask_T() => GetKnownType(context, KnownILStubReference.ValueTask_1).MakeInstantiatedType(context.GetSignatureVariable(0, true));
            TypeDesc Exception() => context.GetWellKnownType(WellKnownType.Exception);
        }
    }
}
