// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Internal.TypeSystem;

namespace Internal.IL.Stubs
{
    public static class AsyncThunkILEmitter
    {
        // The emitted code matches method EmitTaskReturningThunk in CoreCLR VM.
        public static MethodIL EmitTaskReturningThunk(MethodDesc taskReturningMethod, MethodDesc asyncMethod)
        {
            TypeSystemContext context = taskReturningMethod.Context;

            var emitter = new ILEmitter();
            var codestream = emitter.NewCodeStream();

            MethodSignature sig = asyncMethod.Signature;
            TypeDesc returnType = sig.ReturnType;

            MetadataType md = taskReturningMethod.Signature.ReturnType as MetadataType;
            ReadOnlySpan<byte> name = md.Name;
            bool isValueTask = name.SequenceEqual("ValueTask"u8) || name.SequenceEqual("ValueTask`1"u8);

            TypeDesc logicalReturnType = null;
            ILLocalVariable logicalResultLocal = 0;
            if (returnType.HasInstantiation)
            {
                // The return type is either Task<T> or ValueTask<T>, exactly one generic argument
                logicalReturnType = returnType.Instantiation[0];
                logicalResultLocal = emitter.NewLocal(logicalReturnType);
            }

            ILLocalVariable returnTaskLocal = emitter.NewLocal(returnType);

            // TODO: Fix this (ExecutionAndSyncBlockStore is not available in Native AOT).

            // TypeDesc executionAndSyncBlockStoreType = context.SystemModule.GetKnownType("System.Runtime.CompilerServices"u8, "ExecutionAndSyncBlockStore"u8);
            // ILLocalVariable executionAndSyncBlockStoreLocal = emitter.NewLocal(executionAndSyncBlockStoreType);

            ILCodeLabel returnTaskLabel = emitter.NewCodeLabel();
            ILCodeLabel suspendedLabel = emitter.NewCodeLabel();
            ILCodeLabel finishedLabel = emitter.NewCodeLabel();

            // codestream.EmitLdLoca(executionAndSyncBlockStoreLocal);
            // codestream.Emit(ILOpcode.call, emitter.NewToken(executionAndSyncBlockStoreType.GetKnownMethod("Push"u8, null)));

            ILExceptionRegionBuilder tryFinallyRegion = emitter.NewFinallyRegion();
            {
                codestream.BeginTry(tryFinallyRegion);
                codestream.Emit(ILOpcode.nop);
                ILExceptionRegionBuilder tryCatchRegion = emitter.NewCatchRegion();
                {
                    codestream.BeginTry(tryCatchRegion);

                    int localArg = 0;
                    if (sig.IsExplicitThis)
                    {
                        codestream.EmitLdArg(localArg++);
                    }

                    for (int iArg = 0; iArg < sig.Length; iArg++)
                    {
                        codestream.EmitLdArg(localArg++);
                    }

                    if (asyncMethod.OwningType.HasInstantiation)
                    {
                        var inst = new TypeDesc[asyncMethod.OwningType.Instantiation.Length];
                        for (int i = 0; i < inst.Length; i++)
                        {
                            inst[i] = context.GetSignatureVariable(i, false);
                        }

                        var instantiatedType = context.GetInstantiatedType((MetadataType)asyncMethod.OwningType, new Instantiation(inst));
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

                    // TODO: Fix this (AsyncCallContinuation is not available in Native AOT).

                    //MethodDesc asyncCallContinuationMd = context.SystemModule
                    //                            .GetKnownType("System.StubHelpers"u8, "StubHelpers"u8)
                    //                            .GetKnownMethod("AsyncCallContinuation"u8, null);

                    //codestream.Emit(ILOpcode.call, emitter.NewToken(asyncCallContinuationMd));

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
                                .GetKnownType("System.Threading.Tasks"u8, "ValueTask`1"u8)
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

                    codestream.EmitLdLoc(returnTaskLocal);
                    codestream.Emit(ILOpcode.leave, returnTaskLabel);

                    codestream.EndTry(tryCatchRegion);
                }
                // Catch
                {
                    codestream.BeginHandler(tryCatchRegion);

                    MethodDesc fromExceptionMd;
                    if (logicalReturnType != null)
                    {
                        // Generate: returnType.FromException<T>(Exception)
                        if (isValueTask)
                        {
                            fromExceptionMd = context.SystemModule
                                .GetKnownType("System.Threading.Tasks"u8, "ValueTask`1"u8)
                                .GetKnownMethod("FromException"u8, null);
                        }
                        else
                        {
                            fromExceptionMd = context.SystemModule
                                .GetKnownType("System.Threading.Tasks"u8, "Task`1"u8)
                                .GetKnownMethod("FromException"u8, null);
                        }
                        fromExceptionMd = fromExceptionMd.MakeInstantiatedMethod(new Instantiation(logicalReturnType));
                    }
                    else
                    {
                        // Generate: returnType.FromException(Exception)
                        if (isValueTask)
                        {
                            fromExceptionMd = context.SystemModule
                                .GetKnownType("System.Threading.Tasks"u8, "ValueTask"u8)
                                .GetKnownMethod("FromException"u8, null);
                        }
                        else
                        {
                            fromExceptionMd = context.SystemModule
                                .GetKnownType("System.Threading.Tasks"u8, "Task"u8)
                                .GetKnownMethod("FromException"u8, null);
                        }
                    }

                    codestream.Emit(ILOpcode.call, emitter.NewToken(fromExceptionMd));
                    codestream.EmitStLoc(returnTaskLocal);
                    codestream.Emit(ILOpcode.leave, returnTaskLabel);
                    codestream.EndHandler(tryCatchRegion);
                }

                codestream.EmitLabel(suspendedLabel);

                // TODO: Fix this (Finalize returning thunks are not available in Native AOT).

                //MethodDesc finalizeTaskReturningThunkMd;

                //if (logicalReturnType != null)
                //{
                //    if (isValueTask)
                //    {
                //        finalizeTaskReturningThunkMd = context.SystemModule
                //            .GetKnownType("System.Runtime.CompilerServices"u8, "AsyncHelpers"u8)
                //            .GetKnownMethod("FinalizeValueTaskReturningThunk`1"u8, null);
                //    }
                //    else
                //    {
                //        finalizeTaskReturningThunkMd = context.SystemModule
                //            .GetKnownType("System.Runtime.CompilerServices"u8, "AsyncHelpers"u8)
                //            .GetKnownMethod("FinalizeTaskReturningThunk`1"u8, null);
                //    }
                //    finalizeTaskReturningThunkMd = finalizeTaskReturningThunkMd.MakeInstantiatedMethod(new Instantiation(logicalReturnType));
                //}
                //else
                //{
                //    if (isValueTask)
                //    {
                //        finalizeTaskReturningThunkMd = context.SystemModule
                //            .GetKnownType("System.Runtime.CompilerServices"u8, "AsyncHelpers"u8)
                //            .GetKnownMethod("FinalizeValueTaskReturningThunk"u8, null);
                //    }
                //    else
                //    {
                //        finalizeTaskReturningThunkMd = context.SystemModule
                //            .GetKnownType("System.Runtime.CompilerServices"u8, "AsyncHelpers"u8)
                //            .GetKnownMethod("FinalizeTaskReturningThunk"u8, null);
                //    }
                //}
                //codestream.Emit(ILOpcode.call, emitter.NewToken(finalizeTaskReturningThunkMd));
                codestream.EmitStLoc(returnTaskLocal);
                codestream.Emit(ILOpcode.leave, returnTaskLabel);

                codestream.EndTry(tryFinallyRegion);
            }
            //
            {
                codestream.BeginHandler(tryFinallyRegion);

                // TODO: Fix this (ExecutionAndSyncBlockStore is not available in Native AOT).

                // codestream.EmitLdLoca(executionAndSyncBlockStoreLocal);
                // codestream.Emit(ILOpcode.call, emitter.NewToken(executionAndSyncBlockStoreType.GetKnownMethod("Pop"u8, null)));
                codestream.EndHandler(tryFinallyRegion);
            }

            codestream.EmitLabel(returnTaskLabel);
            codestream.EmitLdLoc(returnTaskLocal);
            codestream.Emit(ILOpcode.ret);

            return emitter.Link(taskReturningMethod);
        }
    }
}
