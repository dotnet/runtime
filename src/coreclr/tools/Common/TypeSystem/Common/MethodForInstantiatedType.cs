// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem
{
    public sealed partial class MethodForInstantiatedType : MethodDesc
    {
        private readonly MethodDesc _typicalMethodDef;
        private readonly InstantiatedType _instantiatedType;

        private MethodSignature _signature;

        private MethodDesc _asyncOtherVariant;
        private AsyncMethodData _asyncMethodData;

        internal MethodForInstantiatedType(MethodDesc typicalMethodDef, InstantiatedType instantiatedType)
        {
            Debug.Assert(typicalMethodDef.GetTypicalMethodDefinition() == typicalMethodDef);
            _typicalMethodDef = typicalMethodDef;
            _instantiatedType = instantiatedType;
        }

        // This constructor is a performance optimization - it allows supplying the hash code if it has already
        // been computed prior to the allocation of this type. The supplied hash code still has to match the
        // hash code this type would compute on it's own (and we assert to enforce that).
        internal MethodForInstantiatedType(MethodDesc typicalMethodDef, InstantiatedType instantiatedType, int hashcode)
            : this(typicalMethodDef, instantiatedType)
        {
            SetHashCode(hashcode);
        }

        public override TypeSystemContext Context
        {
            get
            {
                return _typicalMethodDef.Context;
            }
        }

        public override TypeDesc OwningType
        {
            get
            {
                return _instantiatedType;
            }
        }

        private TypeDesc Instantiate(TypeDesc type)
        {
            return type.InstantiateSignature(_instantiatedType.Instantiation, default(Instantiation));
        }

        private MethodSignature InstantiateSignature(MethodSignature template)
        {
            MethodSignatureBuilder builder = new MethodSignatureBuilder(template);
            builder.ReturnType = Instantiate(template.ReturnType);
            for (int i = 0; i < template.Length; i++)
                builder[i] = Instantiate(template[i]);

            return builder.ToSignature();
        }

        private void InitializeSignature()
        {
            MethodSignature template = _typicalMethodDef.Signature;
            _signature = InstantiateSignature(template);

        }

        public override AsyncMethodData AsyncMethodData
        {
            get
            {
                if (!_asyncMethodData.Equals(default(AsyncMethodData)))
                    return _asyncMethodData;

                if (Signature.ReturnsTaskOrValueTask())
                {
                    if (IsAsync)
                    {
                        // The signature should already have been updated to reflect the AsyncCallConv
                        // No need to convert to AsyncCallConv signature
                        _asyncMethodData = new AsyncMethodData { Kind = AsyncMethodKind.AsyncVariantImpl, Signature = Signature };
                    }
                    else
                    {
                        _asyncMethodData = new AsyncMethodData { Kind = AsyncMethodKind.TaskReturning, Signature = Signature };
                    }
                }
                else
                {
                    _asyncMethodData = new AsyncMethodData { Kind = AsyncMethodKind.NotAsync, Signature = Signature };
                }

                return _asyncMethodData;
            }
        }

        public override MethodDesc GetAsyncOtherVariant()
        {
            if (_asyncOtherVariant is null)
            {
                MethodDesc otherVariant = IsAsync ? new TaskReturningAsyncThunk(this, InstantiateSignature(_typicalMethodDef.GetAsyncOtherVariant().Signature)) : new AsyncMethodThunk(this);
                Interlocked.CompareExchange(ref _asyncOtherVariant, otherVariant, null);
            }

            return _asyncOtherVariant;
        }

        public override MethodSignature Signature
        {
            get
            {
                if (_signature == null)
                    InitializeSignature();

                return _signature;
            }
        }

        public override Instantiation Instantiation
        {
            get
            {
                return _typicalMethodDef.Instantiation;
            }
        }

        public override bool IsVirtual
        {
            get
            {
                return _typicalMethodDef.IsVirtual;
            }
        }

        public override bool IsNewSlot
        {
            get
            {
                return _typicalMethodDef.IsNewSlot;
            }
        }

        public override bool IsAbstract
        {
            get
            {
                return _typicalMethodDef.IsAbstract;
            }
        }

        public override bool IsFinal
        {
            get
            {
                return _typicalMethodDef.IsFinal;
            }
        }

        public override bool IsPublic
        {
            get
            {
                return _typicalMethodDef.IsPublic;
            }
        }

        public override bool IsAsync
        {
            get
            {
                return _typicalMethodDef.IsAsync;
            }
        }

        public override bool HasCustomAttribute(string attributeNamespace, string attributeName)
        {
            return _typicalMethodDef.HasCustomAttribute(attributeNamespace, attributeName);
        }

        public override MethodDesc GetTypicalMethodDefinition()
        {
            return _typicalMethodDef;
        }

        public override bool IsDefaultConstructor
        {
            get
            {
                return _typicalMethodDef.IsDefaultConstructor;
            }
        }

        public override bool IsStaticConstructor
        {
            get
            {
                return _typicalMethodDef.IsStaticConstructor;
            }
        }

        public override ReadOnlySpan<byte> Name
        {
            get
            {
                return _typicalMethodDef.Name;
            }
        }
    }
}
