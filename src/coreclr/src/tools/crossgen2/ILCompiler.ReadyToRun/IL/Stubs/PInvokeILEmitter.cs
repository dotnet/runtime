// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

using Internal.TypeSystem;
using Internal.TypeSystem.Interop;
using Internal.JitInterface;

using Debug = System.Diagnostics.Debug;

namespace Internal.IL.Stubs
{
    /// <summary>
    /// Provides method bodies for PInvoke methods
    /// </summary>
    public struct PInvokeILEmitter
    {
        private readonly MethodDesc _targetMethod;
        private readonly Marshaller[] _marshallers;
        private readonly PInvokeMetadata _importMetadata;

        private PInvokeILEmitter(MethodDesc targetMethod)
        {
            Debug.Assert(targetMethod.IsPInvoke);
            _targetMethod = targetMethod;
            _importMetadata = targetMethod.GetPInvokeMethodMetadata();

            _marshallers = InitializeMarshallers(targetMethod, _importMetadata.Flags);
        }

        private static Marshaller[] InitializeMarshallers(MethodDesc targetMethod, PInvokeFlags flags)
        {
            MarshalDirection direction = MarshalDirection.Forward;
            MethodSignature methodSig = targetMethod.Signature;

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
                TypeDesc parameterType = (i == 0) ? methodSig.ReturnType : methodSig[i - 1];  //first item is the return type
                marshallers[i] = Marshaller.CreateMarshaller(parameterType,
                                                    MarshallerType.Argument,
                                                    parameterMetadata.MarshalAsDescriptor,
                                                    direction,
                                                    marshallers,
                                                    parameterMetadata.Index,
                                                    flags,
                                                    parameterMetadata.In,
                                                    parameterMetadata.Out,
                                                    parameterMetadata.Return
                                                    );
            }

            return marshallers;
        }

        private void EmitPInvokeCall(PInvokeILCodeStreams ilCodeStreams)
        {
            ILEmitter emitter = ilCodeStreams.Emitter;
            ILCodeStream callsiteSetupCodeStream = ilCodeStreams.CallsiteSetupCodeStream;
            TypeSystemContext context = _targetMethod.Context;

            TypeDesc nativeReturnType = _marshallers[0].NativeParameterType;
            TypeDesc[] nativeParameterTypes = new TypeDesc[_marshallers.Length - 1];

            MetadataType stubHelpersType = InteropTypes.GetStubHelpers(context);

            // if the SetLastError flag is set in DllImport, clear the error code before doing P/Invoke 
            if (_importMetadata.Flags.SetLastError)
            {
                callsiteSetupCodeStream.Emit(ILOpcode.call, emitter.NewToken(
                            stubHelpersType.GetKnownMethod("ClearLastError", null)));
            }

            for (int i = 1; i < _marshallers.Length; i++)
            {
                nativeParameterTypes[i - 1] = _marshallers[i].NativeParameterType;
            }

            callsiteSetupCodeStream.Emit(ILOpcode.call, emitter.NewToken(
                            stubHelpersType.GetKnownMethod("GetStubContext", null)));
            callsiteSetupCodeStream.Emit(ILOpcode.call, emitter.NewToken(
                            stubHelpersType.GetKnownMethod("GetNDirectTarget", null)));
            
            MethodSignatureFlags unmanagedCallConv = _importMetadata.Flags.UnmanagedCallingConvention;

            MethodSignature nativeSig = new MethodSignature(
                _targetMethod.Signature.Flags | unmanagedCallConv, 0, nativeReturnType,
                nativeParameterTypes);

            callsiteSetupCodeStream.Emit(ILOpcode.calli, emitter.NewToken(nativeSig));

            // if the SetLastError flag is set in DllImport, call the PInvokeMarshal.
            // SaveLastWin32Error so that last error can be used later by calling 
            // PInvokeMarshal.GetLastWin32Error
            if (_importMetadata.Flags.SetLastError)
            {
                callsiteSetupCodeStream.Emit(ILOpcode.call, emitter.NewToken(
                            stubHelpersType.GetKnownMethod("SetLastError", null)));
            }
        }

        private MethodIL EmitIL()
        {
            PInvokeILCodeStreams pInvokeILCodeStreams = new PInvokeILCodeStreams();
            ILEmitter emitter = pInvokeILCodeStreams.Emitter;
            ILCodeStream unmarshallingCodestream = pInvokeILCodeStreams.UnmarshallingCodestream;

            // Marshal the arguments
            for (int i = 0; i < _marshallers.Length; i++)
            {
                _marshallers[i].EmitMarshallingIL(pInvokeILCodeStreams);
            }

            EmitPInvokeCall(pInvokeILCodeStreams);

            _marshallers[0].LoadReturnValue(unmarshallingCodestream);
            unmarshallingCodestream.Emit(ILOpcode.ret);

            return new PInvokeILStubMethodIL((ILStubMethodIL)emitter.Link(_targetMethod), IsStubRequired());
        }

        public static MethodIL EmitIL(MethodDesc method)
        {
            try
            {
                return new PInvokeILEmitter(method).EmitIL();
            }
            catch (NotSupportedException)
            {
                throw new RequiresRuntimeJitException(method);
            }
            catch (InvalidProgramException)
            {
                throw new RequiresRuntimeJitException(method);
            }
        }

        private bool IsStubRequired()
        {
            Debug.Assert(_targetMethod.IsPInvoke);

            if (_importMetadata.Flags.SetLastError)
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
