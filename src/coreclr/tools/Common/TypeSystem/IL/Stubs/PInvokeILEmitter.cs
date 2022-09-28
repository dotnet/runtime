// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.TypeSystem;
using Internal.TypeSystem.Interop;
using Debug = System.Diagnostics.Debug;
using Internal.TypeSystem.Ecma;

namespace Internal.IL.Stubs
{
    /// <summary>
    /// Provides method bodies for PInvoke methods
    /// </summary>
    public struct PInvokeILEmitter
    {
        private readonly MethodDesc _targetMethod;
        private readonly Marshaller[] _marshallers;
        private readonly PInvokeILEmitterConfiguration _pInvokeILEmitterConfiguration;
        private readonly PInvokeMetadata _pInvokeMetadata;
        private readonly PInvokeFlags _flags;
        private readonly InteropStateManager _interopStateManager;

        private PInvokeILEmitter(MethodDesc targetMethod, PInvokeILEmitterConfiguration pinvokeILEmitterConfiguration, InteropStateManager interopStateManager)
        {
            Debug.Assert(targetMethod.IsPInvoke || targetMethod is DelegateMarshallingMethodThunk || targetMethod is CalliMarshallingMethodThunk);
            _targetMethod = targetMethod;
            _pInvokeILEmitterConfiguration = pinvokeILEmitterConfiguration;
            _pInvokeMetadata = targetMethod.GetPInvokeMethodMetadata();
            _interopStateManager = interopStateManager;

            //
            // targetMethod could be either a PInvoke or a DelegateMarshallingMethodThunk
            // ForwardNativeFunctionWrapper method thunks are marked as PInvokes, so it is
            // important to check them first here so that we get the right flags.
            //
            if (_targetMethod is DelegateMarshallingMethodThunk delegateMethod)
            {
                _flags = ((EcmaType)delegateMethod.DelegateType.GetTypeDefinition()).GetDelegatePInvokeFlags();
            }
            else
            {
                _flags = _pInvokeMetadata.Flags;
            }
            _marshallers = InitializeMarshallers(targetMethod, interopStateManager, _flags);
        }

        private static Marshaller[] InitializeMarshallers(MethodDesc targetMethod, InteropStateManager interopStateManager, PInvokeFlags flags)
        {
            MarshalDirection direction = MarshalDirection.Forward;
            MethodSignature methodSig;
            bool runtimeMarshallingEnabled;
            switch (targetMethod)
            {
                case DelegateMarshallingMethodThunk delegateMethod:
                    methodSig = delegateMethod.DelegateSignature;
                    direction = delegateMethod.Direction;
                    runtimeMarshallingEnabled = MarshalHelpers.IsRuntimeMarshallingEnabled(delegateMethod.DelegateType.Module);
                    break;
                case CalliMarshallingMethodThunk calliMethod:
                    methodSig = calliMethod.TargetSignature;
                    runtimeMarshallingEnabled = calliMethod.RuntimeMarshallingEnabled;
                    break;
                default:
                    methodSig = targetMethod.Signature;
                    runtimeMarshallingEnabled = MarshalHelpers.IsRuntimeMarshallingEnabled(((MetadataType)targetMethod.OwningType).Module);
                    break;
            }
            int indexOffset = 0;
            if (!methodSig.IsStatic && direction == MarshalDirection.Forward)
            {
                // For instance methods(eg. Forward delegate marshalling thunk), first argument is
                // the instance
                indexOffset = 1;
            }
            ParameterMetadata[] parameterMetadataArray = targetMethod.GetParameterMetadata();
            Marshaller[] marshallers = new Marshaller[methodSig.Length + 1];
            int parameterIndex = 0;
            ParameterMetadata parameterMetadata;

            for (int i = 0; i < marshallers.Length; i++)
            {
                Debug.Assert(parameterIndex == parameterMetadataArray.Length || i <= parameterMetadataArray[parameterIndex].Index);
                if (parameterIndex == parameterMetadataArray.Length || i < parameterMetadataArray[parameterIndex].Index)
                {
                    // if we don't have metadata for the parameter, create a dummy one
                    parameterMetadata = new ParameterMetadata(i, ParameterMetadataAttributes.None, null);
                }
                else
                {
                    Debug.Assert(i == parameterMetadataArray[parameterIndex].Index);
                    parameterMetadata = parameterMetadataArray[parameterIndex++];
                }

                TypeDesc parameterType;
                bool isHRSwappedRetVal = false;
                if (i == 0)
                {
                    // First item is the return type
                    parameterType = methodSig.ReturnType;
                    if (!flags.PreserveSig && !parameterType.IsVoid)
                    {
                        // PreserveSig = false can only show up an regular forward PInvokes
                        Debug.Assert(direction == MarshalDirection.Forward);

                        parameterType = methodSig.Context.GetByRefType(parameterType);
                        isHRSwappedRetVal = true;
                    }
                }
                else
                {
                    parameterType = methodSig[i - 1];
                }

                if (runtimeMarshallingEnabled)
                {
                    marshallers[i] = Marshaller.CreateMarshaller(parameterType,
                                                        parameterIndex,
                                                        methodSig.GetEmbeddedSignatureData(),
                                                        MarshallerType.Argument,
                                                        parameterMetadata.MarshalAsDescriptor,
                                                        direction,
                                                        marshallers,
                                                        interopStateManager,
                                                        indexOffset + parameterMetadata.Index,
                                                        flags,
                                                        parameterMetadata.In,
                                                        isHRSwappedRetVal ? true : parameterMetadata.Out,
                                                        isHRSwappedRetVal ? false : parameterMetadata.Return
                                                        );
                }
                else
                {
                    marshallers[i] = Marshaller.CreateDisabledMarshaller(
                        parameterType,
                        parameterIndex,
                        MarshallerType.Argument,
                        direction,
                        marshallers,
                        indexOffset + parameterMetadata.Index,
                        flags,
                        parameterMetadata.Return);
                }
            }

            return marshallers;
        }

        private void EmitDelegateCall(DelegateMarshallingMethodThunk delegateMethod, PInvokeILCodeStreams ilCodeStreams)
        {
            ILEmitter emitter = ilCodeStreams.Emitter;
            ILCodeStream fnptrLoadStream = ilCodeStreams.FunctionPointerLoadStream;
            ILCodeStream marshallingCodeStream = ilCodeStreams.MarshallingCodeStream;
            ILCodeStream callsiteSetupCodeStream = ilCodeStreams.CallsiteSetupCodeStream;
            TypeSystemContext context = _targetMethod.Context;

            Debug.Assert(delegateMethod != null);

            if (delegateMethod.Kind == DelegateMarshallingMethodThunkKind.ReverseOpenStatic)
            {
                //
                // For Open static delegates call
                //     InteropHelpers.GetCurrentCalleeOpenStaticDelegateFunctionPointer()
                // which returns a function pointer. Just call the function pointer and we are done.
                //
                TypeDesc[] parameters = new TypeDesc[_marshallers.Length - 1];
                for (int i = 1; i < _marshallers.Length; i++)
                {
                    parameters[i - 1] = _marshallers[i].ManagedParameterType;
                }

                MethodSignature managedSignature = new MethodSignature(
                    MethodSignatureFlags.Static, 0, _marshallers[0].ManagedParameterType, parameters);

                fnptrLoadStream.Emit(ILOpcode.call, emitter.NewToken(
                    delegateMethod.Context.GetHelperType("InteropHelpers").GetKnownMethod(
                        "GetCurrentCalleeOpenStaticDelegateFunctionPointer", null)));

                ILLocalVariable vDelegateStub = emitter.NewLocal(
                    delegateMethod.Context.GetWellKnownType(WellKnownType.IntPtr));

                fnptrLoadStream.EmitStLoc(vDelegateStub);
                callsiteSetupCodeStream.EmitLdLoc(vDelegateStub);
                callsiteSetupCodeStream.Emit(ILOpcode.calli, emitter.NewToken(managedSignature));
            }
            else if (delegateMethod.Kind == DelegateMarshallingMethodThunkKind.ReverseClosed)
            {
                //
                // For closed delegates call
                //     InteropHelpers.GetCurrentCalleeDelegate<Delegate>
                // which returns the delegate. Do a CallVirt on the invoke method.
                //
                MethodDesc instantiatedHelper = delegateMethod.Context.GetInstantiatedMethod(
                    delegateMethod.Context.GetHelperType("InteropHelpers")
                    .GetKnownMethod("GetCurrentCalleeDelegate", null),
                        new Instantiation((delegateMethod.DelegateType)));

                fnptrLoadStream.Emit(ILOpcode.call, emitter.NewToken(instantiatedHelper));

                ILLocalVariable vDelegateStub = emitter.NewLocal(delegateMethod.DelegateType);
                fnptrLoadStream.EmitStLoc(vDelegateStub);
                marshallingCodeStream.EmitLdLoc(vDelegateStub);
                MethodDesc invokeMethod = delegateMethod.DelegateType.GetKnownMethod("Invoke", null);
                callsiteSetupCodeStream.Emit(ILOpcode.callvirt, emitter.NewToken(invokeMethod));
            }
            else if (delegateMethod.Kind == DelegateMarshallingMethodThunkKind.ForwardNativeFunctionWrapper)
            {
                // if the SetLastError flag is set in UnmanagedFunctionPointerAttribute, clear the error code before doing P/Invoke
                if (_flags.SetLastError)
                {
                    callsiteSetupCodeStream.Emit(ILOpcode.call, emitter.NewToken(
                                InteropTypes.GetPInvokeMarshal(context).GetKnownMethod("ClearLastError", null)));
                }

                //
                // For NativeFunctionWrapper we need to load the native function and call it
                //
                fnptrLoadStream.EmitLdArg(0);
                fnptrLoadStream.Emit(ILOpcode.call, emitter.NewToken(InteropTypes
                    .GetNativeFunctionPointerWrapper(context)
                    .GetMethod("get_NativeFunctionPointer", null)));

                var fnPtr = emitter.NewLocal(
                    context.GetWellKnownType(WellKnownType.IntPtr));

                fnptrLoadStream.EmitStLoc(fnPtr);
                callsiteSetupCodeStream.EmitLdLoc(fnPtr);

                TypeDesc nativeReturnType = _marshallers[0].NativeParameterType;
                TypeDesc[] nativeParameterTypes = new TypeDesc[_marshallers.Length - 1];

                for (int i = 1; i < _marshallers.Length; i++)
                {
                    nativeParameterTypes[i - 1] = _marshallers[i].NativeParameterType;
                }

                MethodSignatureFlags unmanagedCallingConvention = _flags.UnmanagedCallingConvention;
                if (unmanagedCallingConvention == MethodSignatureFlags.None)
                    unmanagedCallingConvention = MethodSignatureFlags.UnmanagedCallingConvention;

                MethodSignature nativeSig = new MethodSignature(
                    MethodSignatureFlags.Static | unmanagedCallingConvention, 0, nativeReturnType, nativeParameterTypes);

                callsiteSetupCodeStream.Emit(ILOpcode.calli, emitter.NewToken(nativeSig));

                // if the SetLastError flag is set in UnmanagedFunctionPointerAttribute, call the PInvokeMarshal.SaveLastError
                // so that last error can be used later by calling Marshal.GetLastPInvokeError
                if (_flags.SetLastError)
                {
                    callsiteSetupCodeStream.Emit(ILOpcode.call, emitter.NewToken(
                                InteropTypes.GetPInvokeMarshal(context)
                                .GetKnownMethod("SaveLastError", null)));
                }
            }
            else
            {
                Debug.Fail("Unexpected DelegateMarshallingMethodThunkKind");
            }
        }

        private void EmitPInvokeCall(PInvokeILCodeStreams ilCodeStreams)
        {
            ILEmitter emitter = ilCodeStreams.Emitter;
            ILCodeStream fnptrLoadStream = ilCodeStreams.FunctionPointerLoadStream;
            ILCodeStream callsiteSetupCodeStream = ilCodeStreams.CallsiteSetupCodeStream;
            TypeSystemContext context = _targetMethod.Context;

            bool isHRSwappedRetVal = !_flags.PreserveSig && !_targetMethod.Signature.ReturnType.IsVoid;
            TypeDesc nativeReturnType = _flags.PreserveSig ? _marshallers[0].NativeParameterType : context.GetWellKnownType(WellKnownType.Int32);
            TypeDesc[] nativeParameterTypes = new TypeDesc[isHRSwappedRetVal ? _marshallers.Length : _marshallers.Length - 1];

            bool runtimeMarshallingEnabled = MarshalHelpers.IsRuntimeMarshallingEnabled(((MetadataType)_targetMethod.OwningType).Module);

            // if the SetLastError flag is set in DllImport, clear the error code before doing P/Invoke
            if (_flags.SetLastError)
            {
                if (!runtimeMarshallingEnabled)
                {
                    // When runtime marshalling is disabled, we don't support SetLastError
                    throw new NotSupportedException();
                }
                callsiteSetupCodeStream.Emit(ILOpcode.call, emitter.NewToken(
                            InteropTypes.GetPInvokeMarshal(context).GetKnownMethod("ClearLastError", null)));
            }

            for (int i = 1; i < _marshallers.Length; i++)
            {
                nativeParameterTypes[i - 1] = _marshallers[i].NativeParameterType;
            }

            if (isHRSwappedRetVal)
            {
                if (!runtimeMarshallingEnabled)
                {
                    // When runtime marshalling is disabled, we don't support HResult-return-swapping
                    throw new NotSupportedException();
                }
                nativeParameterTypes[_marshallers.Length - 1] = _marshallers[0].NativeParameterType;
            }

            if (!_pInvokeILEmitterConfiguration.GenerateDirectCall(_targetMethod, out _))
            {
                MetadataType lazyHelperType = context.GetHelperType("InteropHelpers");
                FieldDesc lazyDispatchCell = _interopStateManager.GetPInvokeLazyFixupField(_targetMethod);

                fnptrLoadStream.Emit(ILOpcode.ldsflda, emitter.NewToken(lazyDispatchCell));
                fnptrLoadStream.Emit(ILOpcode.call, emitter.NewToken(lazyHelperType
                    .GetKnownMethod("ResolvePInvoke", null)));

                MethodSignature nativeSig = new MethodSignature(
                    MethodSignatureFlags.Static | MethodSignatureFlags.UnmanagedCallingConvention, 0, nativeReturnType, nativeParameterTypes,
                    _targetMethod.GetPInvokeMethodCallingConventions().EncodeAsEmbeddedSignatureData(context));

                ILLocalVariable vNativeFunctionPointer = emitter.NewLocal(context
                    .GetWellKnownType(WellKnownType.IntPtr));

                fnptrLoadStream.EmitStLoc(vNativeFunctionPointer);
                callsiteSetupCodeStream.EmitLdLoc(vNativeFunctionPointer);
                callsiteSetupCodeStream.Emit(ILOpcode.calli, emitter.NewToken(nativeSig));
            }
            else
            {
                // Eager call
                MethodSignature nativeSig = new MethodSignature(
                    _targetMethod.Signature.Flags, 0, nativeReturnType, nativeParameterTypes);

                MethodDesc nativeMethod =
                    new PInvokeTargetNativeMethod(_targetMethod, nativeSig);

                callsiteSetupCodeStream.Emit(ILOpcode.call, emitter.NewToken(nativeMethod));
            }

            if (!_flags.PreserveSig)
            {
                callsiteSetupCodeStream.Emit(ILOpcode.call, emitter.NewToken(
                    InteropTypes.GetMarshal(context)
                    .GetKnownMethod("ThrowExceptionForHR", null)));
            }

            // if the SetLastError flag is set in DllImport, call the PInvokeMarshal.SaveLastError
            // so that last error can be used later by calling Marshal.GetLastPInvokeError
            if (_flags.SetLastError)
            {
                callsiteSetupCodeStream.Emit(ILOpcode.call, emitter.NewToken(
                            InteropTypes.GetPInvokeMarshal(context)
                            .GetKnownMethod("SaveLastError", null)));
            }
        }

        private void EmitCalli(PInvokeILCodeStreams ilCodeStreams, CalliMarshallingMethodThunk calliThunk)
        {
            ILEmitter emitter = ilCodeStreams.Emitter;
            ILCodeStream callsiteSetupCodeStream = ilCodeStreams.CallsiteSetupCodeStream;

            TypeDesc nativeReturnType = _marshallers[0].NativeParameterType;
            TypeDesc[] nativeParameterTypes = new TypeDesc[_marshallers.Length - 1];

            for (int i = 1; i < _marshallers.Length; i++)
            {
                nativeParameterTypes[i - 1] = _marshallers[i].NativeParameterType;
            }

            MethodSignature nativeSig = new MethodSignature(
                calliThunk.TargetSignature.Flags, 0, nativeReturnType,
                nativeParameterTypes, calliThunk.TargetSignature.GetEmbeddedSignatureData());

            callsiteSetupCodeStream.EmitLdArg(calliThunk.TargetSignature.Length);
            callsiteSetupCodeStream.Emit(ILOpcode.calli, emitter.NewToken(nativeSig));
        }

        private MethodIL EmitIL()
        {
            if (_targetMethod.HasCustomAttribute("System.Runtime.InteropServices", "LCIDConversionAttribute"))
                throw new NotSupportedException();

            PInvokeILCodeStreams pInvokeILCodeStreams = new PInvokeILCodeStreams();
            ILEmitter emitter = pInvokeILCodeStreams.Emitter;
            ILCodeStream marshallingCodestream = pInvokeILCodeStreams.MarshallingCodeStream;
            ILCodeStream unmarshallingCodestream = pInvokeILCodeStreams.UnmarshallingCodestream;
            ILCodeStream cleanupCodestream = pInvokeILCodeStreams.CleanupCodeStream;

            // Marshalling is wrapped in a finally block to guarantee cleanup
            ILExceptionRegionBuilder tryFinally = emitter.NewFinallyRegion();

            marshallingCodestream.BeginTry(tryFinally);
            cleanupCodestream.BeginHandler(tryFinally);

            // Marshal the arguments
            bool isHRSwappedRetVal = !_flags.PreserveSig && !_targetMethod.Signature.ReturnType.IsVoid;
            for (int i = isHRSwappedRetVal ? 1 : 0; i < _marshallers.Length; i++)
            {
                _marshallers[i].EmitMarshallingIL(pInvokeILCodeStreams);
            }

            if (isHRSwappedRetVal)
            {
                _marshallers[0].EmitMarshallingIL(pInvokeILCodeStreams);
            }

            // make the call
            switch (_targetMethod)
            {
                case DelegateMarshallingMethodThunk delegateMethod:
                    EmitDelegateCall(delegateMethod, pInvokeILCodeStreams);
                    break;
                case CalliMarshallingMethodThunk calliMethod:
                    EmitCalli(pInvokeILCodeStreams, calliMethod);
                    break;
                default:
                    EmitPInvokeCall(pInvokeILCodeStreams);
                    break;
            }

            ILCodeLabel lReturn = emitter.NewCodeLabel();
            unmarshallingCodestream.Emit(ILOpcode.leave, lReturn);
            unmarshallingCodestream.EndTry(tryFinally);

            cleanupCodestream.Emit(ILOpcode.endfinally);
            cleanupCodestream.EndHandler(tryFinally);

            cleanupCodestream.EmitLabel(lReturn);

            _marshallers[0].LoadReturnValue(cleanupCodestream);
            cleanupCodestream.Emit(ILOpcode.ret);

            return new PInvokeILStubMethodIL((ILStubMethodIL)emitter.Link(_targetMethod), IsStubRequired());
        }

        public static MethodIL EmitIL(MethodDesc method,
            PInvokeILEmitterConfiguration pinvokeILEmitterConfiguration,
            InteropStateManager interopStateManager)
        {
            try
            {
                return new PInvokeILEmitter(method, pinvokeILEmitterConfiguration, interopStateManager)
                    .EmitIL();
            }
            catch (NotSupportedException)
            {
                string message = "Method '" + method.ToString() +
                    "' requires marshalling that is not yet supported by this compiler.";
                return MarshalHelpers.EmitExceptionBody(message, method);
            }
            catch (InvalidProgramException ex)
            {
                Debug.Assert(!string.IsNullOrEmpty(ex.Message));
                return MarshalHelpers.EmitExceptionBody(ex.Message, method);
            }
        }

        private bool IsStubRequired()
        {
            Debug.Assert(_targetMethod.IsPInvoke || _targetMethod is DelegateMarshallingMethodThunk || _targetMethod is CalliMarshallingMethodThunk);

            if (_targetMethod is DelegateMarshallingMethodThunk)
            {
                return true;
            }

            // The configuration can be null if this is delegate or calli marshalling
            if (_pInvokeILEmitterConfiguration != null)
            {
                if (!_pInvokeILEmitterConfiguration.GenerateDirectCall(_targetMethod, out _))
                {
                    return true;
                }
            }

            if (_flags.SetLastError)
            {
                return true;
            }

            if (!_flags.PreserveSig)
            {
                return true;
            }

            for (int i = 0; i < _marshallers.Length; i++)
            {
                if (_marshallers[i].IsMarshallingRequired())
                    return true;
            }
            return false;
        }
    }

    public sealed class PInvokeILStubMethodIL : ILStubMethodIL
    {
        public bool IsStubRequired { get; }
        public PInvokeILStubMethodIL(ILStubMethodIL methodIL, bool isStubRequired) : base(methodIL)
        {
            IsStubRequired = isStubRequired;
        }
    }
}
