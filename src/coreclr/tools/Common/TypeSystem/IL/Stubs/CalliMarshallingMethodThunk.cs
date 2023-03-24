// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
                    // Prepend fnptr argument to the signature
                    TypeDesc[] parameterTypes = new TypeDesc[_targetSignature.Length + 1];

                    for (int i = 0; i < _targetSignature.Length; i++)
                        parameterTypes[i] = _targetSignature[i];
                    parameterTypes[parameterTypes.Length - 1] = Context.GetWellKnownType(WellKnownType.IntPtr);

                    _signature = new MethodSignature(MethodSignatureFlags.Static, 0, _targetSignature.ReturnType, parameterTypes);
                }
                return _signature;
            }
        }

        public override string Name
        {
            get
            {
                return "CalliMarshallingMethodThunk";
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
