// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Internal.NativeFormat;

namespace Internal.TypeSystem
{
    public sealed partial class InstantiatedMethod : MethodDesc
    {
        private MethodDesc _methodDef;
        private Instantiation _instantiation;

        private MethodSignature _signature;

        internal InstantiatedMethod(MethodDesc methodDef, Instantiation instantiation)
        {
            Debug.Assert(!(methodDef is InstantiatedMethod));
            _methodDef = methodDef;

            Debug.Assert(instantiation.Length > 0);
            _instantiation = instantiation;
        }

        // This constructor is a performance optimization - it allows supplying the hash code if it has already
        // been computed prior to the allocation of this type. The supplied hash code still has to match the
        // hash code this type would compute on it's own (and we assert to enforce that).
        internal InstantiatedMethod(MethodDesc methodDef, Instantiation instantiation, int hashcode)
            : this(methodDef, instantiation)
        {
            SetHashCode(hashcode);
        }

        protected override int ComputeHashCode()
        {
            return TypeHashingAlgorithms.ComputeMethodHashCode(OwningType.GetHashCode(), Instantiation.ComputeGenericInstanceHashCode(TypeHashingAlgorithms.ComputeNameHashCode(Name)));
        }

        public override TypeSystemContext Context
        {
            get
            {
                return _methodDef.Context;
            }
        }

        public override TypeDesc OwningType
        {
            get
            {
                return _methodDef.OwningType;
            }
        }

        private TypeDesc Instantiate(TypeDesc type)
        {
            return type.InstantiateSignature(default(Instantiation), _instantiation);
        }

        public override MethodSignature Signature
        {
            get
            {
                if (_signature == null)
                {
                    MethodSignature template = _methodDef.Signature;
                    MethodSignatureBuilder builder = new MethodSignatureBuilder(template);

                    builder.ReturnType = Instantiate(template.ReturnType);
                    for (int i = 0; i < template.Length; i++)
                        builder[i] = Instantiate(template[i]);

                    _signature = builder.ToSignature();
                }

                return _signature;
            }
        }

        public override Instantiation Instantiation
        {
            get
            {
                return _instantiation;
            }
        }

        public override bool IsVirtual
        {
            get
            {
                return _methodDef.IsVirtual;
            }
        }

        public override bool IsNewSlot
        {
            get
            {
                return _methodDef.IsNewSlot;
            }
        }

        public override bool IsAbstract
        {
            get
            {
                return _methodDef.IsAbstract;
            }
        }

        public override bool IsFinal
        {
            get
            {
                return _methodDef.IsFinal;
            }
        }

        public override bool HasCustomAttribute(string attributeNamespace, string attributeName)
        {
            return _methodDef.HasCustomAttribute(attributeNamespace, attributeName);
        }

        public override bool IsDefaultConstructor
        {
            get
            {
                return false;
            }
        }

        public override MethodDesc GetMethodDefinition()
        {
            return _methodDef;
        }

        public override MethodDesc GetTypicalMethodDefinition()
        {
            return _methodDef.GetTypicalMethodDefinition();
        }

        public override string Name
        {
            get
            {
                return _methodDef.Name;
            }
        }
    }
}
