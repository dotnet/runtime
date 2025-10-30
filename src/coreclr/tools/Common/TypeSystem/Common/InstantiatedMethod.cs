// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading;
using Internal.NativeFormat;

namespace Internal.TypeSystem
{
    public sealed partial class InstantiatedMethod : MethodDesc
    {
        private MethodDesc _methodDef;
        private Instantiation _instantiation;

        private MethodSignature _signature;
        private AsyncMethodData _asyncMethodData;
        private MethodDesc _asyncOtherVariant;

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
            return OwningType.GetHashCode() ^ VersionResilientHashCode.GenericInstanceHashCode(VersionResilientHashCode.NameHashCode(Name), Instantiation);
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

        private void InitializeSignature()
        {
            var template = _methodDef.Signature;
            _signature = InstantiateSignature(template);
        }
        private MethodSignature InstantiateSignature(MethodSignature template)
        {
            var builder = new MethodSignatureBuilder(template);
            builder.ReturnType = Instantiate(template.ReturnType);
            for (int i = 0; i < template.Length; i++)
                builder[i] = Instantiate(template[i]);
            return builder.ToSignature();
        }

        public override AsyncMethodData AsyncMethodData
        {
            get
            {
                if (_asyncMethodData.Equals(default(AsyncMethodData)))
                {
                    if (Signature.ReturnsTaskOrValueTask())
                    {
                        if (IsAsync)
                        {
                            // If the method is already async, the template signature should already have been updated to reflect the AsyncCallConv
                            Debug.Assert(!Signature.ReturnsTaskOrValueTask() && Signature.IsAsyncCallConv);
                            _asyncMethodData = new AsyncMethodData() { Kind = AsyncMethodKind.AsyncVariantImpl, Signature = Signature };
                        }
                        else
                        {
                            _asyncMethodData = new AsyncMethodData() { Kind = AsyncMethodKind.TaskReturning, Signature = Signature };
                        }
                    }
                    else
                    {
                        _asyncMethodData = new AsyncMethodData() { Kind = AsyncMethodKind.NotAsync, Signature = Signature };
                    }
                }

                return _asyncMethodData;
            }
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

        public override MethodDesc GetAsyncOtherVariant()
        {
            if (_asyncOtherVariant is null)
            {
                MethodDesc otherVariant = IsAsync ?
                    new TaskReturningAsyncThunk(this, InstantiateSignature(_methodDef.GetAsyncOtherVariant().Signature))
                    : new AsyncMethodThunk(this);
                Interlocked.CompareExchange(ref _asyncOtherVariant, otherVariant, null);
            }
            return _asyncOtherVariant;
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

        public override bool IsPublic
        {
            get
            {
                return _methodDef.IsPublic;
            }
        }

        public override bool IsAsync
        {
            get
            {
                return _methodDef.IsAsync;
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

        public override bool IsStaticConstructor
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

        public override ReadOnlySpan<byte> Name
        {
            get
            {
                return _methodDef.Name;
            }
        }
    }
}
