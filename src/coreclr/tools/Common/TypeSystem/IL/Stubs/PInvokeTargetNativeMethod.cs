// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace Internal.IL.Stubs
{
    /// <summary>
    /// Synthetic method that represents the actual PInvoke target method.
    /// All parameters are simple types. There will be no code
    /// generated for this method. Instead, a static reference to a symbol will be emitted.
    /// </summary>
    public sealed partial class PInvokeTargetNativeMethod : MethodDesc
    {
        private readonly MethodDesc _declMethod;
        private readonly MethodSignature _signature;

        public MethodDesc Target
        {
            get
            {
                return _declMethod;
            }
        }

        public PInvokeTargetNativeMethod(MethodDesc declMethod, MethodSignature signature)
        {
            _declMethod = declMethod;
            _signature = signature;
        }

        public override TypeSystemContext Context
        {
            get
            {
                return _declMethod.Context;
            }
        }

        public override TypeDesc OwningType
        {
            get
            {
                return _declMethod.OwningType;
            }
        }

        public override MethodSignature Signature
        {
            get
            {
                return _signature;
            }
        }

        public override string Name
        {
            get
            {
                return _declMethod.Name;
            }
        }

        public override bool HasCustomAttribute(string attributeNamespace, string attributeName)
        {
            return false;
        }

        public override bool IsPInvoke
        {
            get
            {
                return true;
            }
        }

        public override bool IsNoInlining
        {
            get
            {
                // This method does not have real IL body. NoInlining stops the JIT asking for it.
                return true;
            }
        }

        public override PInvokeMetadata GetPInvokeMethodMetadata()
        {
            return _declMethod.GetPInvokeMethodMetadata();
        }
    }
}
