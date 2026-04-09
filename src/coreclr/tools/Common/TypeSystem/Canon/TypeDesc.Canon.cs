// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem
{
    // Implements canonicalization for types
    public partial class TypeDesc
    {
        /// <summary>
        /// Stores a cached version of the canonicalized form of this type since
        /// calculating it is a recursive operation
        /// </summary>
        private TypeDesc _specificCanonCache;
        private TypeDesc _universalCanonCache;

        private TypeDesc GetCachedCanonValue(CanonicalFormKind kind)
        {
            switch (kind)
            {
                case CanonicalFormKind.Specific:
                    return _specificCanonCache;

                case CanonicalFormKind.Universal:
                    return _universalCanonCache;

                default:
                    Debug.Fail("Invalid CanonicalFormKind: " + kind);
                    return null;
            }
        }

        private void SetCachedCanonValue(CanonicalFormKind kind, TypeDesc value)
        {
            switch (kind)
            {
                case CanonicalFormKind.Specific:
                    Debug.Assert(_specificCanonCache == null || _specificCanonCache == value);
                    _specificCanonCache = value;
                    break;

                case CanonicalFormKind.Universal:
                    Debug.Assert(_universalCanonCache == null || _universalCanonCache == value);
                    _universalCanonCache = value;
                    break;

                default:
                    Debug.Fail("Invalid CanonicalFormKind: " + kind);
                    break;
            }
        }

        /// <summary>
        /// Returns the canonical form of this type
        /// </summary>
        public TypeDesc ConvertToCanonForm(CanonicalFormKind kind)
        {
            TypeDesc canonForm = GetCachedCanonValue(kind);
            if (canonForm == null)
            {
                canonForm = ConvertToCanonFormImpl(kind);
                SetCachedCanonValue(kind, canonForm);
            }

            return canonForm;
        }

        /// <summary>
        /// Derived types that override this should convert their generic parameters to canonical ones
        /// </summary>
        protected abstract TypeDesc ConvertToCanonFormImpl(CanonicalFormKind kind);

        /// <summary>
        /// Returns true if this type matches the discovery policy or if it's parameterized over one that does.
        /// </summary>
        public abstract bool IsCanonicalSubtype(CanonicalFormKind policy);

        /// <summary>
        /// Gets a value indicating whether this type is considered to be canonical type.
        /// Note this will only return true if this is type is the actual __Canon/__UniversalCanon type,
        /// or a struct instantiated over one of those. See also <see cref="IsCanonicalSubtype(CanonicalFormKind)"/>.
        /// </summary>
        internal bool IsCanonicalType
        {
            get
            {
                if (Context.IsCanonicalDefinitionType(this, CanonicalFormKind.Any))
                    return true;
                else if (this.IsValueType)
                    return this.IsCanonicalSubtype(CanonicalFormKind.Any);
                else
                    return false;
            }
        }
    }
}
