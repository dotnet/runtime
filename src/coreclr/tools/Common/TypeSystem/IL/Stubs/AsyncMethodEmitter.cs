// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if READYTORUN
using System;
using System.Collections.Generic;
using System.Diagnostics;


using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Internal.IL.Stubs;
using System.Buffers.Binary;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Unicode;
using ILCompiler.DependencyAnalysis.ReadyToRun;
using ILCompiler;

namespace Internal.IL.Stubs
{
    /// <summary>
    /// Provides method bodies for Task-returning async wrapper methods
    /// </summary>
    public struct AsyncMethodEmitter
    {
        public AsyncMethodEmitter(MutableModule manifestModule, CompilationModuleGroup compilationModuleGroup)
        {
            _manifestMutableModule = manifestModule;
            _compilationModuleGroup = compilationModuleGroup;
        }
        private Dictionary<EcmaMethod, MethodIL> _manifestModuleWrappedMethods = new Dictionary<EcmaMethod, MethodIL>();
        private MutableModule _manifestMutableModule;
        private CompilationModuleGroup _compilationModuleGroup;

        public MethodIL EmitIL(MethodDesc method)
        {
            // Emits roughly the following code:
            //
            // ExecutionAndSyncBlockStore store = default;
            // store.Push();
            // try
            // {
            //   try
            //   {
            //     T result = Inner(args);
            //     // call an intrisic to see if the call above produced a continuation
            //     if (StubHelpers.AsyncCallContinuation() == null)
            //       return Task.FromResult(result);
            //
            //     return FinalizeTaskReturningThunk();
            //   }
            //   catch (Exception ex)
            //   {
            //     return TaskFromException(ex);
            //   }
            // }
            // finally
            // {
            //   store.Pop();
            // }
            ILEmitter ilEmitter = new ILEmitter();
            ILCodeStream il = ilEmitter.NewCodeStream();
            TypeDesc retType = method.Signature.ReturnType;
            IL.Stubs.ILLocalVariable returnTaskLocal = ilEmitter.NewLocal(retType);
            bool isValueTask = retType.IsValueType;
            TypeDesc logicalResultType = null;
            IL.Stubs.ILLocalVariable logicalResultLocal = default;
            if (retType.HasInstantiation)
            {
                logicalResultType = retType.Instantiation[0];
                logicalResultLocal = ilEmitter.NewLocal(logicalResultType);
            }
            var executionAndSyncBlockStoreType = GetR2RKnownType(R2RKnownType.ExecutionAndSyncBlockStore);
            var executionAndSyncBlockStoreLocal = ilEmitter.NewLocal(executionAndSyncBlockStoreType);

            ILCodeLabel returnTaskLabel = ilEmitter.NewCodeLabel();
            ILCodeLabel suspendedLabel = ilEmitter.NewCodeLabel();
            ILCodeLabel finishedLabel = ilEmitter.NewCodeLabel();

            // store.Push()
            il.EmitLdLoca(executionAndSyncBlockStoreLocal);
            il.Emit(ILOpcode.call, ilEmitter.NewToken(GetR2RKnownMethod(R2RKnownMethod.ExecutionAndSyncBlockStore_Push)));

            // Inner try block must appear first in metadata
            var exceptionType = ilEmitter.NewToken(GetR2RKnownType(R2RKnownType.SystemException));
            ILExceptionRegionBuilder innerTryRegion = ilEmitter.NewCatchRegion(exceptionType);
            // try
            // {
            ILExceptionRegionBuilder outerTryRegion = ilEmitter.NewFinallyRegion();
            il.BeginTry(outerTryRegion);
            il.BeginTry(innerTryRegion);

            // var result = Inner(args)
            int argIndex = 0;
            if (!method.Signature.IsStatic)
            {
                il.EmitLdArg(argIndex++);
            }
            for (int i = 0; i < method.Signature.Length; i++)
            {
                il.EmitLdArg(argIndex++);
            }
            MethodDesc asyncOtherVariant = method.GetAsyncOtherVariant();
            il.Emit(ILOpcode.call, ilEmitter.NewToken(asyncOtherVariant));
            if (logicalResultLocal != default)
            {
                il.EmitStLoc(logicalResultLocal);
            }

            // if (StubHelpers.AsyncCallContinuation() == null) return Task.FromResult(result);
            il.Emit(ILOpcode.call, ilEmitter.NewToken(GetR2RKnownMethod(R2RKnownMethod.StubHelpers_AsyncCallContinuation)));
            il.Emit(ILOpcode.brfalse, finishedLabel);
            il.Emit(ILOpcode.leave, suspendedLabel);
            il.EmitLabel(finishedLabel);
            if (logicalResultLocal != default)
            {
                il.EmitLdLoc(logicalResultLocal);
                R2RKnownMethod fromResultValue = isValueTask ? R2RKnownMethod.ValueTask_FromResult : R2RKnownMethod.Task_FromResult;
                MethodDesc fromResultMethod = GetR2RKnownMethod(fromResultValue).MakeInstantiatedMethod(logicalResultType);
                il.Emit(ILOpcode.call, ilEmitter.NewToken(fromResultMethod));
            }
            else
            {
                MethodDesc completedTaskGetter;
                if (isValueTask)
                {
                    completedTaskGetter = GetR2RKnownMethod(R2RKnownMethod.ValueTask_get_CompletedTask);
                }
                else
                {
                    completedTaskGetter = GetR2RKnownMethod(R2RKnownMethod.Task_get_CompletedTask);
                }
                il.Emit(ILOpcode.call, ilEmitter.NewToken(completedTaskGetter));
            }
            il.EmitStLoc(returnTaskLocal);

            il.Emit(ILOpcode.leave, returnTaskLabel);
            il.EndTry(innerTryRegion);

            // catch (Exception ex)
            // {
            il.BeginHandler(innerTryRegion);
            // return TaskFromException(ex);
            MethodDesc fromExceptionMethod;
            R2RKnownMethod fromExceptionKnownMethod;
            if (isValueTask)
            {
                fromExceptionKnownMethod = logicalResultLocal != default ?
                    R2RKnownMethod.AsyncHelpers_ValueTaskFromExceptionGeneric :
                    R2RKnownMethod.AsyncHelpers_ValueTaskFromException;
            }
            else
            {
                fromExceptionKnownMethod = logicalResultLocal != default ?
                    R2RKnownMethod.AsyncHelpers_TaskFromExceptionGeneric :
                    R2RKnownMethod.AsyncHelpers_TaskFromException;
            }
            fromExceptionMethod = GetR2RKnownMethod(fromExceptionKnownMethod);
            if (logicalResultLocal != default)
            {
                fromExceptionMethod = fromExceptionMethod.MakeInstantiatedMethod(logicalResultType);
            }

            il.Emit(ILOpcode.call, ilEmitter.NewToken(fromExceptionMethod));
            il.EmitStLoc(returnTaskLocal);

            il.Emit(ILOpcode.leave, returnTaskLabel);
            il.EndHandler(innerTryRegion);
            // } // end catch

            il.EmitLabel(suspendedLabel);

            // finally
            // {
            //
            MethodDesc finalizeMethod;
            R2RKnownMethod finalizeKnownMethod;
            if (isValueTask)
            {
                finalizeKnownMethod = logicalResultLocal != default ?
                    R2RKnownMethod.AsyncHelpers_FinalizeValueTaskReturningThunkGeneric :
                    R2RKnownMethod.AsyncHelpers_FinalizeValueTaskReturningThunk;
            }
            else
            {
                finalizeKnownMethod = logicalResultLocal != default ?
                    R2RKnownMethod.AsyncHelpers_FinalizeTaskReturningThunkGeneric :
                    R2RKnownMethod.AsyncHelpers_FinalizeTaskReturningThunk;
            }
            finalizeMethod = GetR2RKnownMethod(finalizeKnownMethod);

            if (logicalResultLocal != default)
            {
                finalizeMethod = finalizeMethod.MakeInstantiatedMethod(logicalResultType);
            }

            il.Emit(ILOpcode.call, ilEmitter.NewToken(finalizeMethod));
            il.EmitStLoc(returnTaskLocal);

            il.Emit(ILOpcode.leave, returnTaskLabel);
            il.EndTry(outerTryRegion);

            // Finally block
            il.BeginHandler(outerTryRegion);
            il.EmitLdLoca(executionAndSyncBlockStoreLocal);
            il.Emit(ILOpcode.call, ilEmitter.NewToken(GetR2RKnownMethod(R2RKnownMethod.ExecutionAndSyncBlockStore_Pop)));
            il.Emit(ILOpcode.endfinally);
            il.EndHandler(outerTryRegion);

            // Return task label
            il.EmitLabel(returnTaskLabel);
            il.EmitLdLoc(returnTaskLocal);
            il.Emit(ILOpcode.ret);

            return ilEmitter.Link(method);
        }

        public enum R2RKnownType
        {
            ExecutionAndSyncBlockStore,
            SystemException,
        }

        public enum R2RKnownMethod
        {
            ExecutionAndSyncBlockStore_Push,
            ExecutionAndSyncBlockStore_Pop,
            ValueTask_FromResult,
            Task_FromResult,
            ValueTask_get_CompletedTask,
            Task_get_CompletedTask,
            StubHelpers_AsyncCallContinuation,
            AsyncHelpers_TaskFromException,
            AsyncHelpers_ValueTaskFromException,
            AsyncHelpers_TaskFromExceptionGeneric,
            AsyncHelpers_ValueTaskFromExceptionGeneric,
            AsyncHelpers_FinalizeTaskReturningThunk,
            AsyncHelpers_FinalizeValueTaskReturningThunk,
            AsyncHelpers_FinalizeTaskReturningThunkGeneric,
            AsyncHelpers_FinalizeValueTaskReturningThunkGeneric,
        }

        public MetadataType GetR2RKnownType(R2RKnownType knownType)
        {
            switch (knownType)
            {
                case R2RKnownType.ExecutionAndSyncBlockStore:
                    return _manifestMutableModule.Context.SystemModule.GetKnownType("System.Runtime.CompilerServices"u8, "ExecutionAndSyncBlockStore"u8);
                case R2RKnownType.SystemException:
                    return _manifestMutableModule.Context.SystemModule.GetKnownType("System"u8, "SystemException"u8);
                default:
                    throw new InvalidOperationException($"Unknown R2R known type: {knownType}");
            }
        }

        public MethodDesc GetR2RKnownMethod(R2RKnownMethod knownMethod)
        {
            TypeSystemContext context = _manifestMutableModule.Context;
            switch (knownMethod)
            {
                case R2RKnownMethod.ExecutionAndSyncBlockStore_Push:
                    return GetR2RKnownType(R2RKnownType.ExecutionAndSyncBlockStore).GetKnownMethod("Push"u8, null);
                case R2RKnownMethod.ExecutionAndSyncBlockStore_Pop:
                    return GetR2RKnownType(R2RKnownType.ExecutionAndSyncBlockStore).GetKnownMethod("Pop"u8, null);
                case R2RKnownMethod.ValueTask_FromResult:
                    return _manifestMutableModule.Context.SystemModule.GetKnownType("System.Threading.Tasks"u8, "ValueTask"u8).GetKnownMethod("FromResult"u8, null);
                case R2RKnownMethod.Task_FromResult:
                    return _manifestMutableModule.Context.SystemModule.GetKnownType("System.Threading.Tasks"u8, "Task"u8).GetKnownMethod("FromResult"u8, null);
                case R2RKnownMethod.ValueTask_get_CompletedTask:
                    return _manifestMutableModule.Context.SystemModule.GetKnownType("System.Threading.Tasks"u8, "ValueTask"u8).GetKnownMethod("get_CompletedTask"u8, null);
                case R2RKnownMethod.Task_get_CompletedTask:
                    return _manifestMutableModule.Context.SystemModule.GetKnownType("System.Threading.Tasks"u8, "Task"u8).GetKnownMethod("get_CompletedTask"u8, null);
                case R2RKnownMethod.StubHelpers_AsyncCallContinuation:
                    return _manifestMutableModule.Context.SystemModule.GetKnownType("System.StubHelpers"u8, "StubHelpers"u8).GetKnownMethod("AsyncCallContinuation"u8, null);
                case R2RKnownMethod.AsyncHelpers_TaskFromException:
                    return _manifestMutableModule.Context.SystemModule.GetKnownType("System.Runtime.CompilerServices"u8, "AsyncHelpers"u8).GetKnownMethod("TaskFromException"u8, new MethodSignature(MethodSignatureFlags.Static, 0, context.SystemModule.GetKnownType("System.Threading.Tasks"u8, "Task"u8), new TypeDesc[] { context.SystemModule.GetKnownType("System"u8, "Exception"u8) }));
                case R2RKnownMethod.AsyncHelpers_ValueTaskFromException:
                    return _manifestMutableModule.Context.SystemModule.GetKnownType("System.Runtime.CompilerServices"u8, "AsyncHelpers"u8).GetKnownMethod("ValueTaskFromException"u8, new MethodSignature(MethodSignatureFlags.Static, 0, context.SystemModule.GetKnownType("System.Threading.Tasks"u8, "ValueTask"u8), new TypeDesc[] { context.SystemModule.GetKnownType("System"u8, "Exception"u8) }));
                case R2RKnownMethod.AsyncHelpers_TaskFromExceptionGeneric:
                    return _manifestMutableModule.Context.SystemModule.GetKnownType("System.Runtime.CompilerServices"u8, "AsyncHelpers"u8).GetKnownMethod("TaskFromException"u8, new MethodSignature(MethodSignatureFlags.Static, 1, context.SystemModule.GetKnownType("System.Threading.Tasks"u8, "Task`1"u8).MakeInstantiatedType(context.GetSignatureVariable(0, method: true)), new TypeDesc[] { context.SystemModule.GetKnownType("System"u8, "Exception"u8) }));
                case R2RKnownMethod.AsyncHelpers_ValueTaskFromExceptionGeneric:
                    return _manifestMutableModule.Context.SystemModule.GetKnownType("System.Runtime.CompilerServices"u8, "AsyncHelpers"u8).GetKnownMethod("ValueTaskFromException"u8, new MethodSignature(MethodSignatureFlags.Static, 1, context.SystemModule.GetKnownType("System.Threading.Tasks"u8, "ValueTask`1"u8).MakeInstantiatedType(context.GetSignatureVariable(0, method: true)), new TypeDesc[] { context.SystemModule.GetKnownType("System"u8, "Exception"u8) }));
                case R2RKnownMethod.AsyncHelpers_FinalizeTaskReturningThunk:
                    return _manifestMutableModule.Context.SystemModule.GetKnownType("System.Runtime.CompilerServices"u8, "AsyncHelpers"u8).GetKnownMethod("FinalizeTaskReturningThunk"u8, new MethodSignature(MethodSignatureFlags.Static, 0, context.SystemModule.GetKnownType("System.Threading.Tasks"u8, "Task"u8), Array.Empty<TypeDesc>()));
                case R2RKnownMethod.AsyncHelpers_FinalizeValueTaskReturningThunk:
                    return _manifestMutableModule.Context.SystemModule.GetKnownType("System.Runtime.CompilerServices"u8, "AsyncHelpers"u8).GetKnownMethod("FinalizeValueTaskReturningThunk"u8, new MethodSignature(MethodSignatureFlags.Static, 0, context.SystemModule.GetKnownType("System.Threading.Tasks"u8, "ValueTask"u8), Array.Empty<TypeDesc>()));
                case R2RKnownMethod.AsyncHelpers_FinalizeTaskReturningThunkGeneric:
                    return _manifestMutableModule.Context.SystemModule.GetKnownType("System.Runtime.CompilerServices"u8, "AsyncHelpers"u8).GetKnownMethod("FinalizeTaskReturningThunk"u8, new MethodSignature(MethodSignatureFlags.Static, 1, context.SystemModule.GetKnownType("System.Threading.Tasks"u8, "Task`1"u8).MakeInstantiatedType(context.GetSignatureVariable(0, method: true)), Array.Empty<TypeDesc>()));
                case R2RKnownMethod.AsyncHelpers_FinalizeValueTaskReturningThunkGeneric:
                    return _manifestMutableModule.Context.SystemModule.GetKnownType("System.Runtime.CompilerServices"u8, "AsyncHelpers"u8).GetKnownMethod("FinalizeValueTaskReturningThunk"u8, new MethodSignature(MethodSignatureFlags.Static, 1, context.SystemModule.GetKnownType("System.Threading.Tasks"u8, "ValueTask`1"u8).MakeInstantiatedType(context.GetSignatureVariable(0, method: true)), Array.Empty<TypeDesc>()));

                default:
                    throw new InvalidOperationException($"Unknown R2R known method: {knownMethod}");
            }
        }

        public void EnsureR2RKnownMethodsAreInManifestModule(ModuleTokenResolver tokenResolver)
        {
            try
            {
                Debug.Assert(_manifestMutableModule.ModuleThatIsCurrentlyTheSourceOfNewReferences == null);
                _manifestMutableModule.ModuleThatIsCurrentlyTheSourceOfNewReferences = _manifestMutableModule.Context.SystemModule;
                _manifestMutableModule.AddingReferencesToR2RKnownTypesAndMethods = true;

                // Force all known R2R methods to be present in the manifest module
                foreach (R2RKnownMethod knownMethod in Enum.GetValues<R2RKnownMethod>())
                {
                    MethodDesc method = GetR2RKnownMethod(knownMethod);
                    if (!_compilationModuleGroup.VersionsWithMethodBody(method))
                    {
                        tokenResolver.AddModuleTokenForMethod(method, new ModuleToken(_manifestMutableModule, _manifestMutableModule.TryGetEntityHandle(method).Value));

                        // This is effectively an assert
                        tokenResolver.GetModuleTokenForMethod(method, allowDynamicallyCreatedReference: true, throwIfNotFound: true);
                    }
                }
                // Force all known R2R types to be present in the manifest module
                foreach (R2RKnownType knownType in Enum.GetValues<R2RKnownType>())
                {
                    TypeDesc type = GetR2RKnownType(knownType);
                    if (!_compilationModuleGroup.VersionsWithType(type))
                    {
                        tokenResolver.AddModuleTokenForType((EcmaType)type, new ModuleToken(_manifestMutableModule, _manifestMutableModule.TryGetEntityHandle(type).Value));
                        tokenResolver.GetModuleTokenForType((EcmaType)type, allowDynamicallyCreatedReference: true, throwIfNotFound: true);
                    }
                }
            }
            finally
            {
                _manifestMutableModule.AddingReferencesToR2RKnownTypesAndMethods = false;
                _manifestMutableModule.ModuleThatIsCurrentlyTheSourceOfNewReferences = null;
            }
        }
    }
}
#endif
