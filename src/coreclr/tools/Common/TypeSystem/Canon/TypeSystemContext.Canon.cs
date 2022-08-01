// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Interlocked = System.Threading.Interlocked;
using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem
{
    // Includes canonicalization objects local to a particular context
    public partial class TypeSystemContext
    {
        private CanonType _canonType;
        /// <summary>
        /// Instance of System.__Canon for this context
        /// </summary>
        public CanonBaseType CanonType
        {
            get
            {
                if (_canonType == null)
                {
                    Interlocked.CompareExchange(ref _canonType, new CanonType(this), null);
                }
                return _canonType;
            }
        }

        private UniversalCanonType _universalCanonType;
        /// <summary>
        /// Instance of System.__UniversalCanon for this context
        /// </summary>
        public CanonBaseType UniversalCanonType
        {
            get
            {
                if (_universalCanonType == null)
                {
                    Interlocked.CompareExchange(ref _universalCanonType, new UniversalCanonType(this), null);
                }
                return _universalCanonType;
            }
        }

        /// <summary>
        /// Returns true if and only if the '<paramref name="type"/>' is __Canon or __UniversalCanon
        /// that matches the <paramref name="kind"/> parameter.
        /// </summary>
        public bool IsCanonicalDefinitionType(TypeDesc type, CanonicalFormKind kind)
        {
            if (kind == CanonicalFormKind.Any)
            {
                return type == CanonType || type == UniversalCanonType;
            }
            else if (kind == CanonicalFormKind.Specific)
            {
                return type == CanonType;
            }
            else
            {
                Debug.Assert(kind == CanonicalFormKind.Universal);
                return type == UniversalCanonType;
            }
        }

        /// <summary>
        /// Converts an instantiation into its canonical form.
        /// </summary>
        public Instantiation ConvertInstantiationToCanonForm(Instantiation instantiation, CanonicalFormKind kind)
        {
            return ConvertInstantiationToCanonForm(instantiation, kind, out _);
        }

        /// <summary>
        /// Converts an instantiation into its canonical form. Returns the canonical instantiation. The '<paramref name="changed"/>'
        /// parameter indicates whether the returned canonical instantiation is different from the specific instantiation
        /// passed as the input.
        /// </summary>
        protected internal virtual Instantiation ConvertInstantiationToCanonForm(Instantiation instantiation, CanonicalFormKind kind, out bool changed)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Converts a constituent of a constructed type to it's canonical form. Note this method is different
        /// from <see cref="TypeDesc.ConvertToCanonForm(CanonicalFormKind)"/>.
        /// </summary>
        protected internal virtual TypeDesc ConvertToCanon(TypeDesc typeToConvert, CanonicalFormKind kind)
        {
            throw new NotSupportedException();
        }

        public abstract bool SupportsCanon { get; }
        public abstract bool SupportsUniversalCanon { get; }

        public MetadataType GetCanonType(string name)
        {
            switch (name)
            {
                case TypeSystem.CanonType.FullName:
                    return CanonType;
                case TypeSystem.UniversalCanonType.FullName:
                    return UniversalCanonType;
            }

            return null;
        }
    }
}
