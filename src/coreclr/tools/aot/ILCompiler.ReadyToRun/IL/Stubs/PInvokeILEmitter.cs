// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Collections.Concurrent;
using System.Collections.Generic;

using ILCompiler;
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
            _marshallers = Marshaller.GetMarshallersForMethod(targetMethod);
        }

        private void EmitPInvokeCall(PInvokeILCodeStreams ilCodeStreams)
        {
            ILEmitter emitter = ilCodeStreams.Emitter;
            ILCodeStream callsiteSetupCodeStream = ilCodeStreams.CallsiteSetupCodeStream;
            TypeSystemContext context = _targetMethod.Context;

            TypeDesc nativeReturnType = _marshallers[0].NativeParameterType;
            TypeDesc[] nativeParameterTypes = new TypeDesc[_marshallers.Length - 1];

            for (int i = 1; i < _marshallers.Length; i++)
            {
                nativeParameterTypes[i - 1] = _marshallers[i].NativeParameterType;
            }

            MethodSignature nativeSig = new MethodSignature(
                _targetMethod.Signature.Flags, 0, nativeReturnType,
                nativeParameterTypes);

            var rawTargetMethod = new PInvokeTargetNativeMethod(_targetMethod, nativeSig);

            callsiteSetupCodeStream.Emit(ILOpcode.call, emitter.NewToken(rawTargetMethod));
        }

        private MethodIL EmitIL()
        {
            if (!_importMetadata.Flags.PreserveSig)
                throw new NotSupportedException();

            if (MarshalHelpers.ShouldCheckForPendingException(_targetMethod.Context.Target, _importMetadata))
                throw new NotSupportedException();

            if (_targetMethod.IsUnmanagedCallersOnly)
                throw new NotSupportedException();

            if (_targetMethod.HasCustomAttribute("System.Runtime.InteropServices", "LCIDConversionAttribute"))
                throw new NotSupportedException();

            if (_importMetadata.Flags.SetLastError)
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
            for (int i = 0; i < _marshallers.Length; i++)
            {
                _marshallers[i].EmitMarshallingIL(pInvokeILCodeStreams);
            }

            EmitPInvokeCall(pInvokeILCodeStreams);

            ILCodeLabel lReturn = emitter.NewCodeLabel();
            unmarshallingCodestream.Emit(ILOpcode.leave, lReturn);
            unmarshallingCodestream.EndTry(tryFinally);

            cleanupCodestream.Emit(ILOpcode.endfinally);
            cleanupCodestream.EndHandler(tryFinally);

            cleanupCodestream.EmitLabel(lReturn);

            _marshallers[0].LoadReturnValue(cleanupCodestream);
            cleanupCodestream.Emit(ILOpcode.ret);

            return new PInvokeILStubMethodIL((ILStubMethodIL)emitter.Link(_targetMethod));
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
    }

    public sealed class PInvokeILStubMethodIL : ILStubMethodIL
    {
        public bool IsMarshallingRequired { get; }

        public PInvokeILStubMethodIL(ILStubMethodIL methodIL) : base(methodIL)
        {
            IsMarshallingRequired = Marshaller.IsMarshallingRequired(methodIL.OwningMethod);
        }
    }
}
