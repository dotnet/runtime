// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.Serialization;

namespace System.Security.Policy
{
    /// <summary>
    ///     Descriptor stored in the Evidence collection to detail the information we have about a type of
    ///     evidence. This descriptor also stores any evidence that's been generated of the specific type.
    /// </summary>
    [Serializable]
    internal sealed class EvidenceTypeDescriptor
    {
        [NonSerialized]
        private bool m_hostCanGenerate;

        [NonSerialized]
        private bool m_generated;

        private EvidenceBase m_hostEvidence;
        private EvidenceBase m_assemblyEvidence;

        // EvidenceTypeDescriptors are stored in Evidence indexed by the type they describe, so this
        // information is redundant.  We keep it around in checked builds to help debugging, but we can drop
        // it from retial builds.
#if _DEBUG
        [NonSerialized]
        private Type m_evidenceType;
#endif // _DEBUG

        public EvidenceTypeDescriptor()
        {
        }

        /// <summary>
        ///     Make a deep copy of a type descriptor
        /// </summary>
        private EvidenceTypeDescriptor(EvidenceTypeDescriptor descriptor)
        {
            Contract.Assert(descriptor != null);

            m_hostCanGenerate = descriptor.m_hostCanGenerate;

            if (descriptor.m_assemblyEvidence != null)
            {
                m_assemblyEvidence = descriptor.m_assemblyEvidence.Clone() as EvidenceBase;
            }
            if (descriptor.m_hostEvidence != null)
            {
                m_hostEvidence = descriptor.m_hostEvidence.Clone() as EvidenceBase;
            }

#if _DEBUG
            m_evidenceType = descriptor.m_evidenceType;
#endif // _DEBUG
        }

        /// <summary>
        ///     Evidence of this type supplied by the assembly
        /// </summary>
        public EvidenceBase AssemblyEvidence
        {
            get { return m_assemblyEvidence; }

            set
            {
                Contract.Assert(value != null);
#if _DEBUG
                Contract.Assert(CheckEvidenceType(value), "Incorrect type of AssemblyEvidence set");
#endif
                m_assemblyEvidence = value;
            }
        }

        /// <summary>
        ///     Flag indicating that we've already attempted to generate this type of evidence
        /// </summary>
        public bool Generated
        {
            get { return m_generated; }

            set
            {
                Contract.Assert(value, "Attempt to clear the Generated flag");
                m_generated = value;
            }
        }

        /// <summary>
        ///     Has the HostSecurityManager has told us that it can potentially generate evidence of this type
        /// </summary>
        public bool HostCanGenerate
        {
            get { return m_hostCanGenerate; }

            set
            {
                Contract.Assert(value, "Attempt to clear HostCanGenerate flag");
                m_hostCanGenerate = value;
            }
        }

        /// <summary>
        ///     Evidence of this type supplied by the CLR or the host
        /// </summary>
        public EvidenceBase HostEvidence
        {
            get { return m_hostEvidence; }

            set
            {
                Contract.Assert(value != null);
#if _DEBUG
                Contract.Assert(CheckEvidenceType(value), "Incorrect type of HostEvidence set");
#endif
                m_hostEvidence = value;
            }
        }

#if _DEBUG
        /// <summary>
        ///     Verify that evidence being stored in this descriptor is of the correct type
        /// </summary>
        private bool CheckEvidenceType(EvidenceBase evidence)
        {
            Contract.Assert(evidence != null);

            ILegacyEvidenceAdapter legacyAdapter = evidence as ILegacyEvidenceAdapter;
            Type storedType = legacyAdapter == null ? evidence.GetType() : legacyAdapter.EvidenceType;

            return m_evidenceType == null || m_evidenceType.IsAssignableFrom(storedType);
        }
#endif // _DEBUG

        /// <summary>
        ///     Make a deep copy of this descriptor
        /// </summary>
        public EvidenceTypeDescriptor Clone()
        {
            return new EvidenceTypeDescriptor(this);
        }

#if _DEBUG
        /// <summary>
        ///     Set the type that this evidence descriptor refers to.
        /// </summary>
        internal void SetEvidenceType(Type evidenceType)
        {
            Contract.Assert(evidenceType != null);
            Contract.Assert(m_evidenceType == null, "Attempt to reset evidence type");

            m_evidenceType = evidenceType;
        }
#endif // _DEBUG
    }
}
