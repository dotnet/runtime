// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Collections.Generic;
using System.Diagnostics;
using Internal.Runtime.Augments;
using Internal.TypeSystem;

namespace Internal.Runtime.TypeLoader
{
    public struct CanonicallyEquivalentEntryLocator
    {
        private RuntimeTypeHandle _typeToFind;
        private RuntimeTypeHandle _genericDefinition;
        private RuntimeTypeHandle[] _genericArgs;
        private DefType _defType;
        private CanonicalFormKind _canonKind;

        public CanonicallyEquivalentEntryLocator(RuntimeTypeHandle typeToFind, CanonicalFormKind kind)
        {
            if (RuntimeAugments.IsGenericType(typeToFind))
            {
                _genericDefinition = RuntimeAugments.GetGenericInstantiation(typeToFind, out _genericArgs);
            }
            else
            {
                _genericArgs = null;
                _genericDefinition = default(RuntimeTypeHandle);
            }

            _typeToFind = typeToFind;
            _canonKind = kind;
            _defType = null;
        }

        internal CanonicallyEquivalentEntryLocator(DefType typeToFind, CanonicalFormKind kind)
        {
            _genericArgs = null;
            _genericDefinition = default(RuntimeTypeHandle);
            _typeToFind = default(RuntimeTypeHandle);
            _canonKind = kind;
            _defType = typeToFind;
        }

        public int LookupHashCode
        {
            get
            {
                if (_defType != null)
                    return _defType.ConvertToCanonForm(_canonKind).GetHashCode();

                if (!_genericDefinition.IsNull())
                    return TypeLoaderEnvironment.Instance.GetCanonicalHashCode(_typeToFind, _canonKind);
                else
                    return _typeToFind.GetHashCode();
            }
        }

        public bool IsCanonicallyEquivalent(RuntimeTypeHandle other)
        {
            if (_defType != null)
            {
                TypeDesc typeToFindAsCanon = _defType.ConvertToCanonForm(_canonKind);
                TypeDesc otherTypeAsTypeDesc = _defType.Context.ResolveRuntimeTypeHandle(other);
                TypeDesc otherTypeAsCanon = otherTypeAsTypeDesc.ConvertToCanonForm(_canonKind);
                return typeToFindAsCanon == otherTypeAsCanon;
            }

            if (!_genericDefinition.IsNull())
            {
                if (RuntimeAugments.IsGenericType(other))
                {
                    RuntimeTypeHandle otherGenericDefinition;
                    RuntimeTypeHandle[] otherGenericArgs;
                    otherGenericDefinition = RuntimeAugments.GetGenericInstantiation(other, out otherGenericArgs);

                    return _genericDefinition.Equals(otherGenericDefinition) && TypeLoaderEnvironment.Instance.CanInstantiationsShareCode(_genericArgs, otherGenericArgs, _canonKind);
                }
                else
                    return false;
            }
            else
                return _typeToFind.Equals(other);
        }

        public bool ConversionToCanonFormIsAChange()
        {
            if (_defType != null)
            {
                return _defType.ConvertToCanonForm(_canonKind) != _defType;
            }

            if (_genericArgs != null)
                return TypeLoaderEnvironment.Instance.ConversionToCanonFormIsAChange(_genericArgs, _canonKind);

            return false;
        }
    }
}
