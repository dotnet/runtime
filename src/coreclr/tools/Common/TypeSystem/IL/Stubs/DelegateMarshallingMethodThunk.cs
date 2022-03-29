// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Internal.TypeSystem;
using Internal.TypeSystem.Interop;
using Debug = System.Diagnostics.Debug;
using Internal.TypeSystem.Ecma;

namespace Internal.IL.Stubs
{
    public enum DelegateMarshallingMethodThunkKind : byte
    {
        ReverseOpenStatic,
        ReverseClosed,
        ForwardNativeFunctionWrapper
    }
    /// <summary>
    /// Thunk to marshal delegate parameters and invoke the appropriate delegate function pointer
    /// </summary>
    public partial class DelegateMarshallingMethodThunk : ILStubMethod
    {
        private readonly TypeDesc _owningType;
        private readonly MetadataType _delegateType;
        private readonly InteropStateManager _interopStateManager;
        private readonly MethodDesc _invokeMethod;
        private MethodSignature _signature;         // signature of the native callable marshalling stub

        public DelegateMarshallingMethodThunkKind Kind
        {
            get;
        }

        public override bool IsPInvoke
        {
            get
            {
                return Kind == DelegateMarshallingMethodThunkKind.ForwardNativeFunctionWrapper;
            }
        }

        public MarshalDirection Direction
        {
            get
            {
                if (Kind == DelegateMarshallingMethodThunkKind.ForwardNativeFunctionWrapper)
                    return MarshalDirection.Forward;
                else
                    return MarshalDirection.Reverse;
            }
        }

        public DelegateMarshallingMethodThunk(MetadataType delegateType, TypeDesc owningType,
                InteropStateManager interopStateManager, DelegateMarshallingMethodThunkKind kind)
        {
            _owningType = owningType;
            _delegateType = delegateType;
            _invokeMethod = delegateType.GetMethod("Invoke", null);
            _interopStateManager = interopStateManager;
            Kind = kind;
        }

        public override TypeSystemContext Context
        {
            get
            {
                return _owningType.Context;
            }
        }

        public override bool IsUnmanagedCallersOnly
        {
            get
            {
                return Direction == MarshalDirection.Reverse;
            }
        }

        public override TypeDesc OwningType
        {
            get
            {
                return _owningType;
            }
        }

        public MetadataType DelegateType
        {
            get
            {
                return _delegateType;
            }
        }

        private TypeDesc GetNativeMethodParameterType(TypeDesc managedType, MarshalAsDescriptor marshalAs, InteropStateManager interopStateManager, bool isReturn, bool isAnsi)
        {
            TypeDesc nativeType;
            try
            {
                nativeType = MarshalHelpers.GetNativeMethodParameterType(managedType, marshalAs, interopStateManager, isReturn, isAnsi);
            }
            catch (NotSupportedException)
            {
                // if marshalling is not supported for this type the generated stubs will emit appropriate
                // error message. We just set native type to be same as managedtype
                nativeType = managedType;
            }
            return nativeType;
        }

        public override MethodSignature Signature
        {
            get
            {
                if (_signature == null)
                {
                    if (Kind == DelegateMarshallingMethodThunkKind.ForwardNativeFunctionWrapper)
                    {
                        _signature = _invokeMethod.Signature;
                    }
                    else
                    {
                        PInvokeFlags flags = default;

                        if (_delegateType is EcmaType ecmaType)
                        {
                            flags = ecmaType.GetDelegatePInvokeFlags();
                        }

                        // Mirror CharSet normalization from Marshaller.CreateMarshaller
                        bool isAnsi = flags.CharSet switch
                        {
                            CharSet.Ansi => true,
                            CharSet.Unicode => false,
                            CharSet.Auto => !_delegateType.Context.Target.IsWindows,
                            _ => true
                        };

                        MethodSignature delegateSignature = _invokeMethod.Signature;
                        TypeDesc[] nativeParameterTypes = new TypeDesc[delegateSignature.Length];
                        ParameterMetadata[] parameterMetadataArray = _invokeMethod.GetParameterMetadata();
                        int parameterIndex = 0;

                        MarshalAsDescriptor marshalAs = null;
                        if (parameterMetadataArray != null && parameterMetadataArray.Length > 0 && parameterMetadataArray[0].Index == 0)
                        {
                            marshalAs = parameterMetadataArray[parameterIndex++].MarshalAsDescriptor;
                        }

                        TypeDesc nativeReturnType = GetNativeMethodParameterType(delegateSignature.ReturnType, 
                            marshalAs,
                            _interopStateManager,
                            isReturn:true,
                            isAnsi:isAnsi);

                        for (int i = 0; i < delegateSignature.Length; i++)
                        {
                            int sequence = i + 1;
                            Debug.Assert(parameterIndex == parameterMetadataArray.Length || sequence <= parameterMetadataArray[parameterIndex].Index);
                            if (parameterIndex == parameterMetadataArray.Length || sequence < parameterMetadataArray[parameterIndex].Index)
                            {
                                // if we don't have metadata for the parameter, marshalAs is null
                                marshalAs = null;
                            }
                            else
                            {
                                Debug.Assert(sequence == parameterMetadataArray[parameterIndex].Index);
                                marshalAs = parameterMetadataArray[parameterIndex++].MarshalAsDescriptor;
                            }
                            bool isByRefType = delegateSignature[i].IsByRef;

                            var managedType = isByRefType ? delegateSignature[i].GetParameterType() : delegateSignature[i];

                            var nativeType = GetNativeMethodParameterType(managedType, 
                                marshalAs,
                                _interopStateManager,
                                isReturn:false,
                                isAnsi:isAnsi);

                            nativeParameterTypes[i] = isByRefType ? nativeType.MakePointerType() : nativeType;
                        }

                        MethodSignatureFlags unmanagedCallingConvention = flags.UnmanagedCallingConvention;
                        if (unmanagedCallingConvention == MethodSignatureFlags.None)
                            unmanagedCallingConvention = MethodSignatureFlags.UnmanagedCallingConvention;

                        _signature = new MethodSignature(MethodSignatureFlags.Static | unmanagedCallingConvention, 0, nativeReturnType, nativeParameterTypes);
                    }
                }
                return _signature;
            }
        }

        public override ParameterMetadata[] GetParameterMetadata()
        {
            return _invokeMethod.GetParameterMetadata();
        }

        public override PInvokeMetadata GetPInvokeMethodMetadata()
        {
            return _invokeMethod.GetPInvokeMethodMetadata();
        }

        public MethodSignature DelegateSignature
        {
            get
            {
                return _invokeMethod.Signature;
            }
        }

        private string NamePrefix
        {
            get
            {
                switch (Kind)
                {
                    case DelegateMarshallingMethodThunkKind.ReverseOpenStatic:
                        return "ReverseOpenStaticDelegateStub";
                    case DelegateMarshallingMethodThunkKind.ReverseClosed:
                        return "ReverseDelegateStub";
                    case DelegateMarshallingMethodThunkKind.ForwardNativeFunctionWrapper:
                        return "ForwardNativeFunctionWrapper";
                    default:
                        System.Diagnostics.Debug.Fail("Unexpected DelegateMarshallingMethodThunkKind.");
                        return String.Empty;
                }
            }
        }

        public override string Name
        {
            get
            {
                return NamePrefix + "__" + DelegateType.Name;
            }
        }

        public override string DiagnosticName
        {
            get
            {
                return NamePrefix + "__" + DelegateType.DiagnosticName;
            }
        }

        public override MethodIL EmitIL()
        {
            return PInvokeILEmitter.EmitIL(this, default(PInvokeILEmitterConfiguration), _interopStateManager);
        }
    }
}
