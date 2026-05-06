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

        public CanonicallyEquivalentEntryLocator(RuntimeTypeHandle typeToFind)
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
            _defType = null;
        }

        internal CanonicallyEquivalentEntryLocator(DefType typeToFind)
        {
            _genericArgs = null;
            _genericDefinition = default(RuntimeTypeHandle);
            _typeToFind = default(RuntimeTypeHandle);
            _defType = typeToFind;
        }

        public int LookupHashCode
        {
            get
            {
                if (_defType != null)
                    return _defType.ConvertToCanonForm(CanonicalFormKind.Specific).GetHashCode();

                if (!_genericDefinition.IsNull())
                    return TypeLoaderEnvironment.Instance.GetCanonicalHashCode(_typeToFind, CanonicalFormKind.Specific);
                else
                    return _typeToFind.GetHashCode();
            }
        }

        public bool IsCanonicallyEquivalent(RuntimeTypeHandle other)
        {
            if (_defType != null)
            {
                TypeDesc typeToFindAsCanon = _defType.ConvertToCanonForm(CanonicalFormKind.Specific);
                TypeDesc otherTypeAsTypeDesc = _defType.Context.ResolveRuntimeTypeHandle(other);
                TypeDesc otherTypeAsCanon = otherTypeAsTypeDesc.ConvertToCanonForm(CanonicalFormKind.Specific);
                return typeToFindAsCanon == otherTypeAsCanon;
            }

            if (!_genericDefinition.IsNull())
            {
                if (RuntimeAugments.IsGenericType(other))
                {
                    RuntimeTypeHandle otherGenericDefinition;
                    RuntimeTypeHandle[] otherGenericArgs;
                    otherGenericDefinition = RuntimeAugments.GetGenericInstantiation(other, out otherGenericArgs);

                    return _genericDefinition.Equals(otherGenericDefinition) && TypeLoaderEnvironment.Instance.CanInstantiationsShareCode(_genericArgs, otherGenericArgs, CanonicalFormKind.Specific);
                }
                else
                    return false;
            }
            else
                return _typeToFind.Equals(other);
        }
    }
}
