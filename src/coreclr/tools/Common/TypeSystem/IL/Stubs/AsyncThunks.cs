// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Internal.TypeSystem;

namespace Internal.IL.Stubs
{
    public static class AsyncThunkILEmitter
    {
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
            codestream.Emit(ILOpcode.call, emitter.NewToken(executionAndSyncBlockStoreType.GetKnownMethod("Push"u8, null)));

            ILExceptionRegionBuilder tryFinallyRegion = emitter.NewFinallyRegion();
            {
                codestream.BeginTry(tryFinallyRegion);
                codestream.Emit(ILOpcode.nop);
                ILExceptionRegionBuilder tryCatchRegion = emitter.NewCatchRegion(context.GetWellKnownType(WellKnownType.Object));
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

                    MethodDesc asyncCallContinuationMd = context.SystemModule
                                                .GetKnownType("System.Runtime.CompilerServices"u8, "AsyncHelpers"u8)
                                                .GetKnownMethod("AsyncCallContinuation"u8, null);

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
                            fromResultMethod = context.SystemModule
                                .GetKnownType("System.Threading.Tasks"u8, "ValueTask"u8)
                                .GetKnownMethod("FromResult"u8, null)
                                .MakeInstantiatedMethod(new Instantiation(logicalReturnType));
                        }
                        else
                        {
                            fromResultMethod = context.SystemModule
                                .GetKnownType("System.Threading.Tasks"u8, "Task"u8)
                                .GetKnownMethod("FromResult"u8, null)
                                .MakeInstantiatedMethod(new Instantiation(logicalReturnType));
                        }

                        codestream.Emit(ILOpcode.call, emitter.NewToken(fromResultMethod));
                    }
                    else
                    {
                        MethodDesc getCompletedTaskMethod;
                        if (isValueTask)
                        {
                            getCompletedTaskMethod = context.SystemModule
                                .GetKnownType("System.Threading.Tasks"u8, "ValueTask"u8)
                                .GetKnownMethod("get_CompletedTask"u8, null);
                        }
                        else
                        {
                            getCompletedTaskMethod = context.SystemModule
                                .GetKnownType("System.Threading.Tasks"u8, "Task"u8)
                                .GetKnownMethod("get_CompletedTask"u8, null);
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

                    TypeDesc exceptionType = context.GetWellKnownType(WellKnownType.Exception);

                    MethodDesc fromExceptionMd;
                    if (logicalReturnType != null)
                    {
                        MethodSignature fromExceptionSignature = new MethodSignature(
                            MethodSignatureFlags.Static,
                            genericParameterCount: 1,
                            returnType: ((MetadataType)returnType.GetTypeDefinition()).MakeInstantiatedType(context.GetSignatureVariable(0, true)),
                            parameters: new[] { exceptionType }
                        );

                        fromExceptionMd = context.SystemModule
                            .GetKnownType("System.Runtime.CompilerServices"u8, "AsyncHelpers"u8)
                            .GetKnownMethod(isValueTask ? "ValueTaskFromException"u8 : "TaskFromException"u8, fromExceptionSignature)
                            .MakeInstantiatedMethod(new Instantiation(logicalReturnType));
                    }
                    else
                    {
                        MethodSignature fromExceptionSignature = new MethodSignature(
                            MethodSignatureFlags.Static,
                            genericParameterCount: 0,
                            returnType: returnType,
                            parameters: new[] { exceptionType }
                        );

                        fromExceptionMd = context.SystemModule
                            .GetKnownType("System.Runtime.CompilerServices"u8, "AsyncHelpers"u8)
                            .GetKnownMethod(isValueTask ? "ValueTaskFromException"u8 : "TaskFromException"u8, fromExceptionSignature);
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
                    MethodSignature finalizeReturningThunkSignature = new MethodSignature(
                        MethodSignatureFlags.Static,
                        genericParameterCount: 1,
                        returnType: ((MetadataType)returnType.GetTypeDefinition()).MakeInstantiatedType(context.GetSignatureVariable(0, true)),
                        parameters: Array.Empty<TypeDesc>()
                    );

                    finalizeTaskReturningThunkMd = context.SystemModule
                        .GetKnownType("System.Runtime.CompilerServices"u8, "AsyncHelpers"u8)
                        .GetKnownMethod(isValueTask ? "FinalizeValueTaskReturningThunk"u8 : "FinalizeTaskReturningThunk"u8, finalizeReturningThunkSignature)
                        .MakeInstantiatedMethod(new Instantiation(logicalReturnType));
                }
                else
                {
                    MethodSignature finalizeReturningThunkSignature = new MethodSignature(
                        MethodSignatureFlags.Static,
                        genericParameterCount: 0,
                        returnType: returnType,
                        parameters: Array.Empty<TypeDesc>()
                    );

                    finalizeTaskReturningThunkMd = context.SystemModule
                        .GetKnownType("System.Runtime.CompilerServices"u8, "AsyncHelpers"u8)
                        .GetKnownMethod(isValueTask ? "FinalizeValueTaskReturningThunk"u8 : "FinalizeTaskReturningThunk"u8, finalizeReturningThunkSignature);
                }

                codestream.Emit(ILOpcode.call, emitter.NewToken(finalizeTaskReturningThunkMd));
                codestream.EmitStLoc(returnTaskLocal);
                codestream.Emit(ILOpcode.leave, returnTaskLabel);

                codestream.EndTry(tryFinallyRegion);
            }

            {
                codestream.BeginHandler(tryFinallyRegion);

                codestream.EmitLdLoca(executionAndSyncBlockStoreLocal);
                codestream.Emit(ILOpcode.call, emitter.NewToken(executionAndSyncBlockStoreType.GetKnownMethod("Pop"u8, null)));
                codestream.Emit(ILOpcode.endfinally);
                codestream.EndHandler(tryFinallyRegion);
            }

            codestream.EmitLabel(returnTaskLabel);
            codestream.EmitLdLoc(returnTaskLocal);
            codestream.Emit(ILOpcode.ret);

            return emitter.Link(taskReturningMethod);
        }

        public static MethodIL EmitAsyncMethodThunk(MethodDesc asyncMethod, MethodDesc taskReturningMethod)
        {
            TypeSystemContext context = asyncMethod.Context;

            var emitter = new ILEmitter();
            var codestream = emitter.NewCodeStream();

            // TODO: match EmitAsyncMethodThunk in CoreCLR VM

            codestream.EmitCallThrowHelper(emitter, context.GetHelperEntryPoint("ThrowHelpers"u8, "ThrowNotSupportedException"u8));

            return emitter.Link(asyncMethod);
        }
    }
}
