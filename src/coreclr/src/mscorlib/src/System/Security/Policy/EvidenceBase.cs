// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Runtime.InteropServices;
#if FEATURE_SERIALIZATION
using System.Runtime.Serialization.Formatters.Binary;
#endif // FEATURE_SERIALIZATION
using System.Security.Permissions;

namespace System.Security.Policy
{
    /// <summary>
    ///     Base class from which all objects to be used as Evidence must derive
    /// </summary>
    [ComVisible(true)]
    [Serializable]
#pragma warning disable 618
    [PermissionSet(SecurityAction.InheritanceDemand, Unrestricted = true)]
#pragma warning restore 618
    public abstract class EvidenceBase
    {
        protected EvidenceBase()
        {
#if FEATURE_SERIALIZATION
            // All objects to be used as evidence must be serializable.  Make sure that any derived types
            // are marked serializable to enforce this, since the attribute does not inherit down to derived
            // classes.
            if (!GetType().IsSerializable)
            {
                throw new InvalidOperationException(Environment.GetResourceString("Policy_EvidenceMustBeSerializable"));
            }
#endif // FEATURE_SERIALIZATION
        }

        /// <remarks>
        ///     Since legacy evidence objects would be cloned by being serialized, the default implementation
        ///     of EvidenceBase will do the same.
        /// </remarks>
#pragma warning disable 618
        [SecurityPermission(SecurityAction.Assert, SerializationFormatter = true)]
        [PermissionSet(SecurityAction.InheritanceDemand, Unrestricted = true)]
#pragma warning restore 618
        [SecuritySafeCritical]
        public virtual EvidenceBase Clone()
        {
#if FEATURE_SERIALIZATION
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(memoryStream, this);

                memoryStream.Position = 0;
                return formatter.Deserialize(memoryStream) as EvidenceBase;
            }
#else // !FEATURE_SERIALIZATION
            throw new NotImplementedException();
#endif // FEATURE_SERIALIZATION
        }
    }

    /// <summary>
    ///     Interface for types which wrap Whidbey evidence objects for compatibility with v4 evidence rules
    /// </summary>
    internal interface ILegacyEvidenceAdapter
    {
        object EvidenceObject { get; }
        Type EvidenceType { get; }
    }

    /// <summary>
    ///     Wrapper class to hold legacy evidence objects which do not derive from EvidenceBase, and allow
    ///     them to be held in the Evidence collection which expects to maintain lists of EvidenceBase only
    /// </summary>
    [Serializable]
    internal sealed class LegacyEvidenceWrapper : EvidenceBase, ILegacyEvidenceAdapter
    {
        private object m_legacyEvidence;

        internal LegacyEvidenceWrapper(object legacyEvidence)
        {
            Contract.Assert(legacyEvidence != null);
            Contract.Assert(legacyEvidence.GetType() != typeof(EvidenceBase), "Attempt to wrap an EvidenceBase in a LegacyEvidenceWrapper");
            Contract.Assert(legacyEvidence.GetType().IsSerializable, "legacyEvidence.GetType().IsSerializable");

            m_legacyEvidence = legacyEvidence;
        }

        public object EvidenceObject
        {
            get { return m_legacyEvidence; }
        }

        public Type EvidenceType
        {
            get { return m_legacyEvidence.GetType(); }
        }

        public override bool Equals(object obj)
        {
            return m_legacyEvidence.Equals(obj);
        }

        public override int GetHashCode()
        {
            return m_legacyEvidence.GetHashCode();
        }

#pragma warning disable 618
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
#pragma warning restore 618
        [SecuritySafeCritical]
        public override EvidenceBase Clone()
        {
            return base.Clone();
        }
    }

    /// <summary>
    ///     Pre-v4 versions of the runtime allow multiple pieces of evidence that all have the same type.
    ///     This type wraps those evidence objects into a single type of list, allowing legacy code to continue
    ///     to work with the Evidence collection that does not expect multiple evidences of the same type.
    ///     
    ///     This may not be limited to LegacyEvidenceWrappers, since it's valid for legacy code to add multiple
    ///     objects of built-in evidence to an Evidence collection.  The built-in evidence now derives from
    ///     EvienceObject, so when the legacy code runs on v4, it may end up attempting to add multiple
    ///     Hash evidences for intsance.
    /// </summary>
    [Serializable]
    internal sealed class LegacyEvidenceList : EvidenceBase, IEnumerable<EvidenceBase>, ILegacyEvidenceAdapter
    {
        private List<EvidenceBase> m_legacyEvidenceList = new List<EvidenceBase>();

        public object EvidenceObject
        {
            get
            {
                // We'll choose the first item in the list to represent us if we're forced to return only
                // one object. This can occur if multiple pieces of evidence are added via the legacy APIs,
                // and then the new APIs are used to retrieve that evidence.
                return m_legacyEvidenceList.Count > 0 ? m_legacyEvidenceList[0] : null;
            }
        }

        public Type EvidenceType
        {
            get
            {
                Contract.Assert(m_legacyEvidenceList.Count > 0, "No items in LegacyEvidenceList, cannot tell what type they are");

                ILegacyEvidenceAdapter adapter = m_legacyEvidenceList[0] as ILegacyEvidenceAdapter;
                return adapter == null ? m_legacyEvidenceList[0].GetType() : adapter.EvidenceType;
            }
        }

        public void Add(EvidenceBase evidence)
        {
            Contract.Assert(evidence != null);
            Contract.Assert(m_legacyEvidenceList.Count == 0 || EvidenceType == evidence.GetType() || (evidence is LegacyEvidenceWrapper && (evidence as LegacyEvidenceWrapper).EvidenceType == EvidenceType),
                            "LegacyEvidenceList must be homogeonous");
            Contract.Assert(evidence.GetType() != typeof(LegacyEvidenceList),
                            "Attempt to add a legacy evidence list to another legacy evidence list");

            m_legacyEvidenceList.Add(evidence);
        }

        public IEnumerator<EvidenceBase> GetEnumerator()
        {
            return m_legacyEvidenceList.GetEnumerator();
        }

        IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return m_legacyEvidenceList.GetEnumerator();
        }

#pragma warning disable 618
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
#pragma warning restore 618
        [SecuritySafeCritical]
        public override EvidenceBase Clone()
        {
            return base.Clone();
        }
    }
}
