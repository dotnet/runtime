// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;

using Internal.Runtime.CompilerServices;
using Internal.Runtime.TypeLoader;

namespace Internal.TypeSystem.NoMetadata
{
    /// <summary>
    /// Represents a method within the Redhawk runtime
    /// </summary>
    internal sealed partial class RuntimeMethodDesc : NoMetadataMethodDesc
    {
        public RuntimeMethodDesc(bool unboxingStub, DefType owningType,
            MethodNameAndSignature nameAndSignature, int hashcode)
        {
            _owningType = owningType;
            _nameAndSignature = nameAndSignature;
            _unboxingStub = unboxingStub;
            SetHashCode(hashcode);

#if DEBUG
            DebugName = this.ToString();
#endif
        }

        public override TypeSystemContext Context
        {
            get
            {
                return _owningType.Context;
            }
        }

        private Instantiation _instantiation;

        public override Instantiation Instantiation
        {
            get
            {
                if (_instantiation.IsNull)
                {
                    uint genericArgCount = TypeLoaderEnvironment.Instance.GetGenericArgumentCountFromMethodNameAndSignature(_nameAndSignature);
                    if (genericArgCount == 0)
                    {
                        _instantiation = Instantiation.Empty;
                    }
                    else
                    {
                        TypeDesc[] genericParameters = new TypeDesc[genericArgCount];
                        for (int i = 0; i < genericParameters.Length; i++)
                        {
                            var newGenericParameter = new RuntimeGenericParameterDesc(GenericParameterKind.Method, i, this, GenericVariance.None);
                            genericParameters[i] = newGenericParameter;
                        }
                        _instantiation = new Instantiation(genericParameters);
                    }
                }
                return _instantiation;
            }
        }

        private DefType _owningType;
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
                throw new NotSupportedException();
            }
        }

        private MethodNameAndSignature _nameAndSignature;
        public override MethodNameAndSignature NameAndSignature
        {
            get
            {
                return _nameAndSignature;
            }
        }

        public override string Name
        {
            get
            {
                return _nameAndSignature.Name;
            }
        }

        private bool _unboxingStub;
        public override bool UnboxingStub
        {
            get
            {
                return _unboxingStub;
            }
        }

        public override MethodDesc GetTypicalMethodDefinition()
        {
            TypeDesc owningTypeDefinition = OwningType.GetTypeDefinition();

            // If this method is on a type that is its own type definition, this it is the type method
            if (owningTypeDefinition == OwningType)
            {
                return this;
            }

            // Otherwise, find its equivalent on the type definition of the owning type
            return Context.ResolveRuntimeMethod(UnboxingStub, (DefType)owningTypeDefinition, _nameAndSignature, IntPtr.Zero, false);
        }

        public override MethodDesc InstantiateSignature(Instantiation typeInstantiation, Instantiation methodInstantiation)
        {
            MethodDesc method = this;

            TypeDesc owningType = method.OwningType;
            TypeDesc instantiatedOwningType = owningType.InstantiateSignature(typeInstantiation, methodInstantiation);
            if (owningType != instantiatedOwningType)
                method = instantiatedOwningType.Context.ResolveRuntimeMethod(UnboxingStub, (DefType)instantiatedOwningType, _nameAndSignature, IntPtr.Zero, false);

            Instantiation instantiation = method.Instantiation;
            TypeDesc[] clone = null;

            for (int i = 0; i < instantiation.Length; i++)
            {
                TypeDesc uninst = instantiation[i];
                TypeDesc inst = uninst.InstantiateSignature(typeInstantiation, methodInstantiation);
                if (inst != uninst)
                {
                    if (clone == null)
                    {
                        clone = new TypeDesc[instantiation.Length];
                        for (int j = 0; j < clone.Length; j++)
                        {
                            clone[j] = instantiation[j];
                        }
                    }
                    clone[i] = inst;
                }
            }

            return (clone == null) ? method : method.Context.GetInstantiatedMethod(method.GetMethodDefinition(), new Instantiation(clone));
        }

        public override bool HasCustomAttribute(string attributeNamespace, string attributeName)
        {
            throw new PlatformNotSupportedException();
        }

#if DEBUG
        public string DebugName;

        public override string ToString()
        {
            string result = OwningType.ToString() + ".Method(" + NameAndSignature.Name + ")";
            return result;
        }
#endif
    }
}
