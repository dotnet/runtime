// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.Text;
using Internal.TypeSystem;

namespace Internal.IL.Stubs
{
    /// <summary>
    /// Thunk to marshal calli PInvoke parameters and invoke the appropriate function pointer
    /// </summary>
    public partial class CalliMarshallingMethodThunk : ILStubMethod
    {
        private readonly MethodSignature _targetSignature;
        private readonly InteropStateManager _interopStateManager;
        private readonly TypeDesc _owningType;

        private MethodSignature _signature;

        public CalliMarshallingMethodThunk(MethodSignature targetSignature, TypeDesc owningType,
                InteropStateManager interopStateManager,
                bool runtimeMarshallingEnabled)
        {
            _targetSignature = targetSignature;
            _owningType = owningType;
            _interopStateManager = interopStateManager;
            RuntimeMarshallingEnabled = runtimeMarshallingEnabled;
        }

        public MethodSignature TargetSignature
        {
            get
            {
                return _targetSignature;
            }
        }

        public override TypeSystemContext Context
        {
            get
            {
                return _owningType.Context;
            }
        }

        public override TypeDesc OwningType
        {
            get
            {
                return _owningType;
            }
        }

        public override MethodSignature Signature
        {
            get
            {
                if (_signature == null)
                {
                    // Append the unmanaged target to the signature.
                    TypeDesc[] parameterTypes = new TypeDesc[_targetSignature.Length + 1];

                    for (int i = 0; i < _targetSignature.Length; i++)
                        parameterTypes[i] = _targetSignature[i];
                    parameterTypes[parameterTypes.Length - 1] = Context.GetWellKnownType(WellKnownType.IntPtr);

                    EmbeddedSignatureData[] embeddedSignatureData =
                    [
                        new()
                        {
                            index = MethodSignature.GetIndexOfCustomModifierOnTypeByParameterIndex(parameterTypes.Length),
                            kind = EmbeddedSignatureDataKind.RequiredCustomModifier,
                            type = Context.SystemModule.GetKnownType(
                                "System.Runtime.CompilerServices"u8,
                                "SecretStubArgument"u8)
                        }
                    ];

                    _signature = new MethodSignature(
                        MethodSignatureFlags.Static,
                        0,
                        _targetSignature.ReturnType,
                        parameterTypes,
                        embeddedSignatureData);
                }
                return _signature;
            }
        }

        public override Utf8Span Name
        {
            get
            {
                return "CalliMarshallingMethodThunk"u8;
            }
        }

        public override string DiagnosticName
        {
            get
            {
                return "CalliMarshallingMethodThunk";
            }
        }

        public bool RuntimeMarshallingEnabled { get; }

        public override PInvokeMetadata GetPInvokeMethodMetadata()
        {
            // Return PInvokeAttributes.PreserveSig to circumvent marshalling required checks
            return new PInvokeMetadata(null, null, PInvokeAttributes.PreserveSig);
        }

        public override MethodIL EmitIL()
        {
            return PInvokeILEmitter.EmitIL(this, default(PInvokeILEmitterConfiguration), _interopStateManager);
        }
    }
}
