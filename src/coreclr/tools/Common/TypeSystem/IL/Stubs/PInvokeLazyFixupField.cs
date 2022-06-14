// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace Internal.IL.Stubs
{
    /// <summary>
    /// Synthetic RVA static field that represents PInvoke fixup cell. The RVA data is
    /// backed by a small data structure generated on the fly from the <see cref="PInvokeMetadata"/>
    /// carried by the instance of this class.
    /// </summary>
    public sealed partial class PInvokeLazyFixupField : FieldDesc
    {
        private readonly DefType _owningType;
        private readonly MethodDesc _targetMethod;

        public PInvokeLazyFixupField(DefType owningType, MethodDesc targetMethod)
        {
            Debug.Assert(targetMethod.IsPInvoke);
            _owningType = owningType;
            _targetMethod = targetMethod;
        }

        public MethodDesc TargetMethod
        {
            get
            {
                return _targetMethod;
            }
        }

        public PInvokeMetadata PInvokeMetadata
        {
            get
            {
                return _targetMethod.GetPInvokeMethodMetadata();
            }
        }

        public override TypeSystemContext Context
        {
            get
            {
                return _targetMethod.Context;
            }
        }

        public override TypeDesc FieldType
        {
            get
            {
                return Context.GetHelperType("InteropHelpers").GetNestedType("MethodFixupCell");
            }
        }

        public override EmbeddedSignatureData[] GetEmbeddedSignatureData() => null;

        public override bool HasRva
        {
            get
            {
                return true;
            }
        }

        public override bool IsInitOnly
        {
            get
            {
                return false;
            }
        }

        public override bool IsLiteral
        {
            get
            {
                return false;
            }
        }

        public override bool IsStatic
        {
            get
            {
                return true;
            }
        }

        public override bool IsThreadStatic
        {
            get
            {
                return false;
            }
        }

        public override DefType OwningType
        {
            get
            {
                return _owningType;
            }
        }

        public override bool HasCustomAttribute(string attributeNamespace, string attributeName)
        {
            return false;
        }

        public override string Name
        {
            get
            {
                return _targetMethod.Name;
            }
        }
    }
}
