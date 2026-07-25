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
        // it calls CreateRuntimeAsyncTask/CreateRuntimeAsyncValueTask method to get the Task/ValueTask.

        // The emitted code matches method EmitTaskReturningThunk in CoreCLR VM.
        public static MethodIL EmitTaskReturningThunk(MethodDesc taskReturningMethod, MethodDesc asyncMethod)
        {
            TypeSystemContext context = taskReturningMethod.Context;

            var emitter = new ILEmitter();
            emitter.SetHasGeneratedTokens();
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

            MetadataType asyncHelpersType = context.SystemModule.GetKnownType("System.Runtime.CompilerServices"u8, "AsyncHelpers"u8);
            TypeDesc stackStateType = asyncHelpersType.GetKnownNestedType("RuntimeAsyncStackState"u8);
            ILLocalVariable stackStateLocal = emitter.NewLocal(stackStateType);
            TypeDesc awaitStateType = asyncHelpersType.GetKnownNestedType("RuntimeAsyncAwaitState"u8);
            ILLocalVariable refAwaitStateLocal = emitter.NewLocal(awaitStateType.MakeByRefType());

            ILCodeLabel returnTaskLabel = emitter.NewCodeLabel();
            ILCodeLabel suspendedLabel = emitter.NewCodeLabel();
            ILCodeLabel finishedLabel = emitter.NewCodeLabel();

            codestream.Emit(ILOpcode.ldsflda, emitter.NewToken(asyncHelpersType.GetKnownField("t_runtimeAsyncAwaitState"u8)));
            codestream.EmitStLoc(refAwaitStateLocal);

            codestream.EmitLdLoc(refAwaitStateLocal);
            codestream.EmitLdLoca(stackStateLocal);
            codestream.Emit(ILOpcode.call, emitter.NewToken(awaitStateType.GetKnownMethod("Push"u8, null)));

            ILExceptionRegionBuilder tryFinallyRegion = emitter.NewFinallyRegion();
            {
                codestream.BeginTry(tryFinallyRegion);
                codestream.Emit(ILOpcode.nop);
                ILExceptionRegionBuilder tryCatchRegion = emitter.NewCatchRegion(context.GetWellKnownType(WellKnownType.Exception));
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

                    MethodDesc asyncCallContinuationMd = asyncHelpersType.GetKnownMethod("AsyncCallContinuation"u8, null);

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

                        fromExceptionMd = asyncHelpersType
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

                        fromExceptionMd = asyncHelpersType
                            .GetKnownMethod(isValueTask ? "ValueTaskFromException"u8 : "TaskFromException"u8, fromExceptionSignature);
                    }

                    codestream.Emit(ILOpcode.call, emitter.NewToken(fromExceptionMd));
                    codestream.EmitStLoc(returnTaskLocal);
                    codestream.Emit(ILOpcode.leave, returnTaskLabel);
                    codestream.EndHandler(tryCatchRegion);
                }

                codestream.EmitLabel(suspendedLabel);

                MethodDesc createRuntimeAsyncTaskMd;
                if (logicalReturnType != null)
                {
                    MethodSignature createRuntimeAsyncTaskSignature = new MethodSignature(
                        MethodSignatureFlags.Static,
                        genericParameterCount: 1,
                        returnType: ((MetadataType)returnType.GetTypeDefinition()).MakeInstantiatedType(context.GetSignatureVariable(0, true)),
                        parameters: [awaitStateType.MakeByRefType()]
                    );

                    createRuntimeAsyncTaskMd = asyncHelpersType
                        .GetKnownMethod(isValueTask ? "CreateRuntimeAsyncValueTask"u8 : "CreateRuntimeAsyncTask"u8, createRuntimeAsyncTaskSignature)
                        .MakeInstantiatedMethod(new Instantiation(logicalReturnType));
                }
                else
                {
                    MethodSignature createRuntimeAsyncTaskSignature = new MethodSignature(
                        MethodSignatureFlags.Static,
                        genericParameterCount: 0,
                        returnType: returnType,
                        parameters: [awaitStateType.MakeByRefType()]
                    );

                    createRuntimeAsyncTaskMd = asyncHelpersType
                        .GetKnownMethod(isValueTask ? "CreateRuntimeAsyncValueTask"u8 : "CreateRuntimeAsyncTask"u8, createRuntimeAsyncTaskSignature);
                }

                codestream.EmitLdLoc(refAwaitStateLocal);
                codestream.Emit(ILOpcode.call, emitter.NewToken(createRuntimeAsyncTaskMd));
                codestream.EmitStLoc(returnTaskLocal);
                codestream.Emit(ILOpcode.leave, returnTaskLabel);

                codestream.EndTry(tryFinallyRegion);
            }

            {
                codestream.BeginHandler(tryFinallyRegion);

                codestream.EmitLdLoc(refAwaitStateLocal);
                codestream.Emit(ILOpcode.call, emitter.NewToken(awaitStateType.GetKnownMethod("Pop"u8, null)));
                codestream.Emit(ILOpcode.endfinally);
                codestream.EndHandler(tryFinallyRegion);
            }

            codestream.EmitLabel(returnTaskLabel);
            codestream.EmitLdLoc(returnTaskLocal);
            codestream.Emit(ILOpcode.ret);

            return emitter.Link(taskReturningMethod);
        }

        // Provided an async variant, emits an async wrapper that drops the returned value.
        // Used in the covariant return scenario.
        // The emitted code matches EmitReturnDroppingThunk in CoreCLR VM.
        public static MethodIL EmitReturnDroppingThunk(MethodDesc returnDroppingMethod, MethodDesc asyncVariantTarget)
        {
            TypeSystemContext context = returnDroppingMethod.Context;

            var emitter = new ILEmitter();
            emitter.SetHasGeneratedTokens();

            var codestream = emitter.NewCodeStream();

            if (asyncVariantTarget.OwningType.HasInstantiation)
            {
                var instantiatedType = (InstantiatedType)TypeSystemHelpers.InstantiateAsOpen(asyncVariantTarget.OwningType);
                asyncVariantTarget = context.GetMethodForInstantiatedType(asyncVariantTarget, instantiatedType);
            }

            if (asyncVariantTarget.HasInstantiation)
            {
                var inst = new TypeDesc[asyncVariantTarget.Instantiation.Length];
                for (int i = 0; i < inst.Length; i++)
                {
                    inst[i] = context.GetSignatureVariable(i, true);
                }
                asyncVariantTarget = asyncVariantTarget.MakeInstantiatedMethod(new Instantiation(inst));
            }

            MethodSignature sig = returnDroppingMethod.Signature;

            // Implement IL that is effectively the following:
            // {
            //    this.other(arg);
            //    return;
            // }

            int localArg = 0;
            codestream.EmitLdArg(localArg++);

            for (int iArg = 0; iArg < sig.Length; iArg++)
            {
                codestream.EmitLdArg(localArg++);
            }

            // Use 'call' not 'callvirt': in NativeAOT the target of this thunk is resolved
            // per type at compile time, so there is no need to redispatch through the vtable.
            // CoreCLR uses callvirt because thunks can be inherited by subtypes at runtime.
            codestream.Emit(ILOpcode.call, emitter.NewToken(asyncVariantTarget));
            codestream.Emit(ILOpcode.pop);
            codestream.Emit(ILOpcode.ret);

            return emitter.Link(returnDroppingMethod);
        }
    }
}
