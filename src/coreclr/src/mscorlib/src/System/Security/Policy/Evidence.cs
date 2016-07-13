// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

namespace System.Security.Policy
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Configuration.Assemblies;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.Remoting;
#if FEATURE_SERIALIZATION
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;
#endif // FEATURE_SERIALIZATION
    using System.Security.Permissions;
    using System.Security.Util;
    using System.Threading;
    using Microsoft.Win32.SafeHandles;

    /// <summary>
    ///     The Evidence class keeps track of information that can be used to make security decisions about
    ///     an assembly or an AppDomain.  There are two types of evidence, one is supplied by the CLR or a
    ///     host, the other supplied by the assembly itself.
    ///     
    ///     We keep a dictionary that maps each type of possbile evidence to an EvidenceTypeDescriptor which
    ///     contains the evidence objects themselves if they exist as well as some extra metadata about that
    ///     type of evidence.  This dictionary is fully populated with keys for host evidence at all times and
    ///     for assembly evidence the first time the application evidence is touched.  This means that if a
    ///     Type key does not exist in the dictionary, then that particular type of evidence will never be
    ///     given to the assembly or AppDomain in question as host evidence.  The only exception is if the
    ///     user later manually adds host evidence via the AddHostEvidence API.
    ///     
    ///     Assembly supplied evidence is created up front, however host supplied evidence may be lazily
    ///     created.  In the lazy creation case, the Type will map to either an EvidenceTypeDescriptor that does
    ///     not contain any evidence data or null.  As requests come in for that evidence, we'll populate the
    ///     EvidenceTypeDescriptor appropriately.
    /// </summary>
#if FEATURE_SERIALIZATION
    [Serializable]
#endif
    [ComVisible(true)]
    public sealed class Evidence
#if FEATURE_CAS_POLICY
 : ICollection
#endif // FEATURE_CAS_POLICY
    {
#if !FEATURE_CORECLR && FEATURE_RWLOCK
#if FEATURE_SERIALIZATION
        [OptionalField(VersionAdded = 4)]
        private Dictionary<Type, EvidenceTypeDescriptor> m_evidence;

        [OptionalField(VersionAdded = 4)]
        private bool m_deserializedTargetEvidence;

        // These fields are only used to deserialize v2.0 serialized versions of Evidence. It will be null
        // after the seriailzation process is complete, and should not be used.
#pragma warning disable 414
        private volatile ArrayList m_hostList;
        private volatile ArrayList m_assemblyList;
#pragma warning restore 414
#else // !FEATURE_SERIALIZATION
        private Dictionary<Type, EvidenceTypeDescriptor> m_evidence;
#endif // FEATURE_SERIALIZATION

        [NonSerialized]
        private ReaderWriterLock m_evidenceLock;

        [NonSerialized]
        private uint m_version;

        [NonSerialized]
        private IRuntimeEvidenceFactory m_target;

        private bool m_locked;

        // If this evidence collection is a clone where we may need to backpatch to the original, this will
        // reference the collection it was cloned from.  See
        // code:System.Security.Policy.Evidence#BackpatchGeneratedEvidence 
        [NonSerialized]
        private WeakReference m_cloneOrigin;

        private static volatile Type[] s_runtimeEvidenceTypes;

        /// <summary>
        ///     Set of actions that we could perform if we detect that we are attempting to add evidence
        ///     when we already have evidence of that type stored.
        /// </summary>
        private enum DuplicateEvidenceAction
        {
            Throw,                  // Throw an exception
            Merge,                  // Create a list of all the evidence objects
            SelectNewObject         // The newly added object wins
        }

#if FEATURE_CAS_POLICY
        public Evidence()
        {
            m_evidence = new Dictionary<Type, EvidenceTypeDescriptor>();
            m_evidenceLock = new ReaderWriterLock();
        }
#endif // FEATURE_CAS_POLICY

        /// <summary>
        ///     Create a deep copy of an evidence object
        /// </summary>
        public Evidence(Evidence evidence)
        {
            m_evidence = new Dictionary<Type, EvidenceTypeDescriptor>();

            if (evidence != null)
            {
                using (EvidenceLockHolder lockHolder = new EvidenceLockHolder(evidence, EvidenceLockHolder.LockType.Reader))
                {
                    foreach (KeyValuePair<Type, EvidenceTypeDescriptor> evidenceType in evidence.m_evidence)
                    {
                        EvidenceTypeDescriptor cloneDescriptor = evidenceType.Value;
                        if (cloneDescriptor != null)
                        {
                            cloneDescriptor = cloneDescriptor.Clone();
                        }

                        m_evidence[evidenceType.Key] = cloneDescriptor;
                    }

                    m_target = evidence.m_target;
                    m_locked = evidence.m_locked;
#if FEATURE_SERIALIZATION
                    m_deserializedTargetEvidence = evidence.m_deserializedTargetEvidence;
#endif // FEATURE_SERIALIZATION

                    // see code:System.Security.Policy.Evidence#BackpatchGeneratedEvidence
                    if (evidence.Target != null)
                    {
                        m_cloneOrigin = new WeakReference(evidence);
                    }
                }
            }

            // see code:System.Security.Policy.Evidence#EvidenceLock
            m_evidenceLock = new ReaderWriterLock();
        }

        [Obsolete("This constructor is obsolete. Please use the constructor which takes arrays of EvidenceBase instead.")]
        public Evidence(object[] hostEvidence, object[] assemblyEvidence)
        {
            m_evidence = new Dictionary<Type, EvidenceTypeDescriptor>();

            // This is a legacy evidence entry point, so we add through the legacy add APIs in order to get
            // proper legacy wrapping and merge behavior.
#pragma warning disable 618
            if (hostEvidence != null)
            {
                foreach (object hostEvidenceObject in hostEvidence)
                {
                    AddHost(hostEvidenceObject);
                }
            }

            if (assemblyEvidence != null)
            {
                foreach (object assemblyEvidenceObject in assemblyEvidence)
                {
                    AddAssembly(assemblyEvidenceObject);
                }
            }
#pragma warning restore 618

            // see code:System.Security.Policy.Evidence#EvidenceLock
            m_evidenceLock = new ReaderWriterLock();
        }

        public Evidence(EvidenceBase[] hostEvidence, EvidenceBase[] assemblyEvidence)
        {
            m_evidence = new Dictionary<Type, EvidenceTypeDescriptor>();

            if (hostEvidence != null)
            {
                foreach (EvidenceBase hostEvidenceObject in hostEvidence)
                {
                    AddHostEvidence(hostEvidenceObject, GetEvidenceIndexType(hostEvidenceObject), DuplicateEvidenceAction.Throw);
                }
            }

            if (assemblyEvidence != null)
            {
                foreach (EvidenceBase assemblyEvidenceObject in assemblyEvidence)
                {
                    AddAssemblyEvidence(assemblyEvidenceObject, GetEvidenceIndexType(assemblyEvidenceObject), DuplicateEvidenceAction.Throw);
                }
            }

            // see code:System.Security.Policy.Evidence#EvidenceLock
            m_evidenceLock = new ReaderWriterLock();
        }

        /// <summary>
        ///     Create an empty evidence collection which will contain evidence for a specific assembly or
        ///     AppDomain
        /// </summary>
        [SecuritySafeCritical]
        internal Evidence(IRuntimeEvidenceFactory target)
        {
            Contract.Assert(target != null);

            m_evidence = new Dictionary<Type, EvidenceTypeDescriptor>();
            m_target = target;

            // Setup the types of evidence that the CLR can generate for a target as keys in the dictionary
            foreach (Type runtimeEvidenceType in RuntimeEvidenceTypes)
            {
                BCLDebug.Assert(typeof(EvidenceBase).IsAssignableFrom(runtimeEvidenceType), "All runtime evidence types should be EvidenceBases");
                m_evidence[runtimeEvidenceType] = null;
            }

            QueryHostForPossibleEvidenceTypes();

            // see code:System.Security.Policy.Evidence#EvidenceLock
            m_evidenceLock = new ReaderWriterLock();
        }

        internal static Type[] RuntimeEvidenceTypes
        {
            get
            {
                if (s_runtimeEvidenceTypes == null)
                {
                    Type[] runtimeEvidenceTypes = new Type[]
                    {
#if FEATURE_CLICKONCE
                        typeof(System.Runtime.Hosting.ActivationArguments),
#endif // FEATURE_CLICKONCE
#if FEATURE_CAS_POLICY
                        typeof(ApplicationDirectory),
#endif // FEATURE_CAS_POLICY
                        typeof(ApplicationTrust),
#if FEATURE_CAS_POLICY
                        typeof(GacInstalled),
                        typeof(Hash),
                        typeof(Publisher),
#endif // FEATURE_CAS_POLICY
                        typeof(Site),
                        typeof(StrongName),
                        typeof(Url),
                        typeof(Zone)
                    };

#if FEATURE_CAS_POLICY
                    // We only supply permission request evidence in legacy CAS mode
                    if (AppDomain.CurrentDomain.IsLegacyCasPolicyEnabled)
                    {
#pragma warning disable 618 // We need to generate PermissionRequestEvidence in compatibility mode
                        int l = runtimeEvidenceTypes.Length;
                        Array.Resize(ref runtimeEvidenceTypes, l+1);
                        runtimeEvidenceTypes[l] = typeof(PermissionRequestEvidence);
#pragma warning restore 618
                    }
#endif // FEATURE_CAS_POLICY

                    s_runtimeEvidenceTypes = runtimeEvidenceTypes;
                }

                return s_runtimeEvidenceTypes;
            }
        }

        //
        // #EvidenceLock
        // 
        // Evidence synchronization locking wrappers. In the case where the lock has not yet been created,
        // we know that we're in the process of constructing the evidence collection and therefore we can
        // act as though the evidence is locked.  If there is a lock in place, then just delegate back to it.
        //
        // The nested EvidenceLockHolder and EvidenceUpgradeLockHolder utility classes can be used to wrap
        // these methods when acquiring and releasing the evidence lock.
        //

        // Millisecond timeout when waiting to acquire the evidence lock
        private const int LockTimeout = 5000;

        private bool IsReaderLockHeld
        {
            get { return m_evidenceLock == null || m_evidenceLock.IsReaderLockHeld; }
        }

        private bool IsWriterLockHeld
        {
            get { return m_evidenceLock == null || m_evidenceLock.IsWriterLockHeld; }
        }

        private void AcquireReaderLock()
        {
            Contract.Assert(m_evidenceLock == null || !IsReaderLockHeld);

            if (m_evidenceLock != null)
            {
                m_evidenceLock.AcquireReaderLock(LockTimeout);
            }
        }

        private void AcquireWriterlock()
        {
            Contract.Assert(m_evidenceLock == null || !IsWriterLockHeld);

            if (m_evidenceLock != null)
            {
                m_evidenceLock.AcquireWriterLock(LockTimeout);
            }
        }

        private void DowngradeFromWriterLock(ref LockCookie lockCookie)
        {
            Contract.Assert(IsWriterLockHeld);
            if (m_evidenceLock != null)
            {
                m_evidenceLock.DowngradeFromWriterLock(ref lockCookie);
            }
        }

        private LockCookie UpgradeToWriterLock()
        {
            Contract.Assert(IsReaderLockHeld);
            return m_evidenceLock != null ? m_evidenceLock.UpgradeToWriterLock(LockTimeout) : new LockCookie();
        }

        private void ReleaseReaderLock()
        {
            Contract.Assert(IsReaderLockHeld);

            if (m_evidenceLock != null)
            {
                m_evidenceLock.ReleaseReaderLock();
            }
        }

        private void ReleaseWriterLock()
        {
            Contract.Assert(IsWriterLockHeld);

            if (m_evidenceLock != null)
            {
                m_evidenceLock.ReleaseWriterLock();
            }
        }

        [Obsolete("This method is obsolete. Please use AddHostEvidence instead.")]
        [SecuritySafeCritical]
        public void AddHost(object id)
        {
            if (id == null)
                throw new ArgumentNullException("id");
            if (!id.GetType().IsSerializable)
                throw new ArgumentException(Environment.GetResourceString("Policy_EvidenceMustBeSerializable"), "id");
            Contract.EndContractBlock();

            if (m_locked)
            {
                new SecurityPermission(SecurityPermissionFlag.ControlEvidence).Demand();
            }

            EvidenceBase evidence = WrapLegacyEvidence(id);
            Type evidenceIndex = GetEvidenceIndexType(evidence);

            // Whidbey allowed for multiple types of the same evidence, so if we're being called via the Whidbey
            // APIs, then allow the evidences to merge together.
            AddHostEvidence(evidence, evidenceIndex, DuplicateEvidenceAction.Merge);
        }

        [Obsolete("This method is obsolete. Please use AddAssemblyEvidence instead.")]
        public void AddAssembly(object id)
        {
            if (id == null)
                throw new ArgumentNullException("id");
            if (!id.GetType().IsSerializable)
                throw new ArgumentException(Environment.GetResourceString("Policy_EvidenceMustBeSerializable"), "id");
            Contract.EndContractBlock();

            EvidenceBase evidence = WrapLegacyEvidence(id);
            Type evidenceIndex = GetEvidenceIndexType(evidence);

            // Whidbey allowed for multiple types of the same evidence, so if we're being called via the Whidbey
            // APIs, then allow the evidences to merge together.
            AddAssemblyEvidence(evidence, evidenceIndex, DuplicateEvidenceAction.Merge);
        }

        /// <summary>
        ///     Add a piece of evidence to the assembly supplied evidence list. This method will disallow adding
        ///     evidence if there is already evidence of that type in the assembly list.
        /// </summary>
        [ComVisible(false)]
        public void AddAssemblyEvidence<T>(T evidence) where T : EvidenceBase
        {
            if (evidence == null)
                throw new ArgumentNullException("evidence");
            Contract.EndContractBlock();

            // Index the evidence under the type that the Add function was called with, unless we were given
            // a plain EvidenceBase or a wrapped legacy evidence.  In that case, we need to index under a
            // more specific type.
            Type evidenceType = typeof(T);
            if (typeof(T) == typeof(EvidenceBase) || evidence is ILegacyEvidenceAdapter)
            {
                evidenceType = GetEvidenceIndexType(evidence);
            }

            AddAssemblyEvidence(evidence, evidenceType, DuplicateEvidenceAction.Throw);
        }

        private void AddAssemblyEvidence(EvidenceBase evidence, Type evidenceType, DuplicateEvidenceAction duplicateAction)
        {
            using (EvidenceLockHolder lockHolder = new EvidenceLockHolder(this, EvidenceLockHolder.LockType.Writer))
            {
                AddAssemblyEvidenceNoLock(evidence, evidenceType, duplicateAction);
            }
        }

        private void AddAssemblyEvidenceNoLock(EvidenceBase evidence, Type evidenceType, DuplicateEvidenceAction duplicateAction)
        {
            Contract.Assert(IsWriterLockHeld);
            Contract.Assert(evidence != null);
            Contract.Assert(evidenceType != null);

            // We need to make sure that any target supplied evidence is deserialized before adding to the
            // Assembly collection in order to preserve the semantics that the evidence objects supplied by
            // the target are the original versions and evidence objects added via the APIs are the duplicates.
            DeserializeTargetEvidence();

            EvidenceTypeDescriptor descriptor = GetEvidenceTypeDescriptor(evidenceType, true);

            ++m_version;
            if (descriptor.AssemblyEvidence == null)
            {
                descriptor.AssemblyEvidence = evidence;
            }
            else
            {
                descriptor.AssemblyEvidence = HandleDuplicateEvidence(descriptor.AssemblyEvidence,
                                                                      evidence,
                                                                      duplicateAction);
            }
        }

        /// <summary>
        ///     Add a piece of evidence to the host supplied evidence list. This method will disallow adding
        ///     evidence if there is already evidence of that type in the host list.
        /// </summary>
        [ComVisible(false)]
        public void AddHostEvidence<T>(T evidence) where T : EvidenceBase
        {
            if (evidence == null)
                throw new ArgumentNullException("evidence");
            Contract.EndContractBlock();

            // Index the evidence under the type that the Add function was called with, unless we were given
            // a plain EvidenceBase or a wrapped legacy evidence.  In that case, we need to index under a
            // more specific type.
            Type evidenceType = typeof(T);
            if (typeof(T) == typeof(EvidenceBase) || evidence is ILegacyEvidenceAdapter)
            {
                evidenceType = GetEvidenceIndexType(evidence);
            }

            AddHostEvidence(evidence, evidenceType, DuplicateEvidenceAction.Throw);
        }

        [SecuritySafeCritical]
        private void AddHostEvidence(EvidenceBase evidence, Type evidenceType, DuplicateEvidenceAction duplicateAction)
        {
            Contract.Assert(evidence != null);
            Contract.Assert(evidenceType != null);

            if (Locked)
            {
                new SecurityPermission(SecurityPermissionFlag.ControlEvidence).Demand();
            }

            using (EvidenceLockHolder lockHolder = new EvidenceLockHolder(this, EvidenceLockHolder.LockType.Writer))
            {
                AddHostEvidenceNoLock(evidence, evidenceType, duplicateAction);
            }
        }

        /// <summary>
        ///     Add evidence to the host supplied evidence collection without acquiring the evidence lock or
        ///     checking to make sure that the caller has permission to bypass locked evidence.
        /// </summary>
        private void AddHostEvidenceNoLock(EvidenceBase evidence, Type evidenceType, DuplicateEvidenceAction duplicateAction)
        {
            Contract.Assert(IsWriterLockHeld);
            Contract.Assert(evidence != null);
            Contract.Assert(evidenceType != null);

            EvidenceTypeDescriptor descriptor = GetEvidenceTypeDescriptor(evidenceType, true);

            ++m_version;
            if (descriptor.HostEvidence == null)
            {
                descriptor.HostEvidence = evidence;
            }
            else
            {
                descriptor.HostEvidence = HandleDuplicateEvidence(descriptor.HostEvidence,
                                                                  evidence,
                                                                  duplicateAction);
            }
        }

        /// <summary>
        ///     Ask the host for the types of evidence that it might provide if it is asked.
        ///     
        ///     This should only be called when setting up the Evidence collection to interact with the
        ///     host, and should not be used once that connection is established and the evidence has been
        ///     made available to user code.
        /// </summary>
        [SecurityCritical]
        private void QueryHostForPossibleEvidenceTypes()
        {
#if FEATURE_CAS_POLICY
            Contract.Assert(IsWriterLockHeld);

            // First check to see if we have a HostSecurityManager
            if (AppDomain.CurrentDomain.DomainManager != null)
            {
                HostSecurityManager hsm = AppDomain.CurrentDomain.DomainManager.HostSecurityManager;
                if (hsm != null)
                {
                    Type[] hostSuppliedTypes = null;

                    AppDomain targetDomain = m_target.Target as AppDomain;
                    Assembly targetAssembly = m_target.Target as Assembly;

                    //
                    // If the HostSecurityManager wants to supply evidence for the type of target that we have,
                    // then ask it what types of evidence it might supply.
                    //

                    if (targetAssembly != null &&
                        (hsm.Flags & HostSecurityManagerOptions.HostAssemblyEvidence) == HostSecurityManagerOptions.HostAssemblyEvidence)
                    {
                        hostSuppliedTypes = hsm.GetHostSuppliedAssemblyEvidenceTypes(targetAssembly);
                    }
                    else if (targetDomain != null &&
                             (hsm.Flags & HostSecurityManagerOptions.HostAppDomainEvidence) == HostSecurityManagerOptions.HostAppDomainEvidence)
                    {
                        hostSuppliedTypes = hsm.GetHostSuppliedAppDomainEvidenceTypes();
                    }

                    //
                    // Finally, mark the descriptor for each of the types that the host can supply to indicate
                    // we should ask the host to generate them if we're asked.
                    // 

                    if (hostSuppliedTypes != null)
                    {
                        foreach (Type hostEvidenceType in hostSuppliedTypes)
                        {
                            EvidenceTypeDescriptor evidenceDescriptor = GetEvidenceTypeDescriptor(hostEvidenceType, true);
                            evidenceDescriptor.HostCanGenerate = true;
                        }
                    }
                }
            }
#endif // FEATURE_CAS_POLICY
        }

        internal bool IsUnmodified
        {
            get { return m_version == 0; }
        }

        /// <summary>
        ///     Set or check to see if the evidence is locked.  Locked evidence cannot have its host supplied
        ///     evidence list be modified without a successful demand for ControlEvidence.  Any code can lock
        ///     evidence, but only code with ControlEvidence may unlock it.
        ///     
        ///     This lock is not the same as the synchronization lock that gates access to the evidence collection.
        /// </summary>
        public bool Locked
        {
            get
            {
                return m_locked;
            }

            [SecuritySafeCritical]
            set
            {
                if (!value)
                {
                    new SecurityPermission(SecurityPermissionFlag.ControlEvidence).Demand();

                    m_locked = false;
                }
                else
                {
                    m_locked = true;
                }
            }
        }

        /// <summary>
        ///     Target of any delay generated evidence objects
        /// </summary>
        internal IRuntimeEvidenceFactory Target
        {
            get { return m_target; }

            //
            // There are two retargeting scenarios supported:
            // 
            //   1. A PEFileEvidenceFactory is being upgraded to an AssemblyEvidenceFactory and we don't want
            //      to throw away any already generated evidence.
            //   2. A detached evidence collection is being applied to an AppDomain and that domain has a
            //      HostSecurityManager. In that case, we want to attach the target to the AppDomain to
            //      allow the HostSecurityManager to get callbacks for delay generated evidence.
            // 

            [SecurityCritical]
            set
            {
#if FEATURE_CAS_POLICY
                Contract.Assert((m_target != null && m_target is PEFileEvidenceFactory && value != null && value is AssemblyEvidenceFactory) ||
                                (m_target == null && value != null && value is AppDomainEvidenceFactory),
                                "Evidence retargeting should only be from PEFile -> Assembly or detached -> AppDomain.");
#endif // FEATURE_CAS_POLICY

                using (EvidenceLockHolder lockHolder = new EvidenceLockHolder(this, EvidenceLockHolder.LockType.Writer))
                {
                    m_target = value;

                    // Since we've updated what we're pointing at, we need to query the host to determine what
                    // types of evidence that it can generate for this new target.
                    QueryHostForPossibleEvidenceTypes();
                }
            }
        }

        /// <summary>
        ///     Get the type that would be used to index into the evidence dictionary for this object
        /// </summary>
        private static Type GetEvidenceIndexType(EvidenceBase evidence)
        {
            Contract.Assert(evidence != null);

            //
            // Legacy wrapper evidence types should be indexed via the type of evidence that they're wrapping
            // so check to see if we have one of those; otherwise just return the type itself.
            //

            ILegacyEvidenceAdapter adapter = evidence as ILegacyEvidenceAdapter;
            return adapter == null ? evidence.GetType() : adapter.EvidenceType;
        }

        /// <summary>
        ///     Get the type descriptor for a specific type of evidence.  This method should be used instead
        ///     of accessing the dictionary directly as it will handle the case where a new descriptor needs
        ///     to be created.
        /// </summary>
        internal EvidenceTypeDescriptor GetEvidenceTypeDescriptor(Type evidenceType)
        {
            return GetEvidenceTypeDescriptor(evidenceType, false);
        }

        /// <summary>
        ///     Get the type descriptor for a specific type of evidence, optionally creating a descriptor if
        ///     we did not yet know about this type of evidence.  This method should be used instead of
        ///     accessing the dictionary directly as it will handle the case where a new descriptor needs
        ///     to be created.
        /// </summary>
        private EvidenceTypeDescriptor GetEvidenceTypeDescriptor(Type evidenceType, bool addIfNotExist)
        {
            Contract.Assert(IsReaderLockHeld || IsWriterLockHeld);
            Contract.Assert(evidenceType != null);

            // If we don't know about the type being indexed and we don't want to add it then exit out
            EvidenceTypeDescriptor descriptor = null;
            if (!m_evidence.TryGetValue(evidenceType, out descriptor) && !addIfNotExist)
            {
                return null;
            }

            // If we haven't yet created a descriptor for this type then create one now
            if (descriptor == null)
            {
                descriptor = new EvidenceTypeDescriptor();
#if _DEBUG
                descriptor.SetEvidenceType(evidenceType);
#endif // _DEBUG

                bool upgradedLock = false;
                LockCookie upgradeCookie = new LockCookie();
                try
                {
                    if (!IsWriterLockHeld)
                    {
                        upgradeCookie = UpgradeToWriterLock();
                        upgradedLock = true;
                    }

                    m_evidence[evidenceType] = descriptor;
                }
                finally
                {
                    if (upgradedLock)
                        DowngradeFromWriterLock(ref upgradeCookie);
                }
            }

            return descriptor;
        }

        /// <summary>
        ///     This method is called if a piece of evidence is added but another piece of evidence of the same
        ///     type already existed.  We have different strategies depending on compatibility concerns of the
        ///     calling code.
        /// </summary>
        private static EvidenceBase HandleDuplicateEvidence(EvidenceBase original,
                                                            EvidenceBase duplicate,
                                                            DuplicateEvidenceAction action)
        {
            Contract.Assert(original != null);
            Contract.Assert(duplicate != null);
            Contract.Assert(original.GetType() == duplicate.GetType() || original.GetType() == typeof(LegacyEvidenceList));

            switch (action)
            {
                // Throw - duplicate evidence is not allowed (Arrowhead behavior), so throw an exception
                case DuplicateEvidenceAction.Throw:
                    throw new InvalidOperationException(Environment.GetResourceString("Policy_DuplicateEvidence", duplicate.GetType().FullName));

                // SelectNewObject - MergeWithNoDuplicates behavior - the duplicate object wins
                case DuplicateEvidenceAction.SelectNewObject:
                    return duplicate;

                // Merge - compat behavior. Merge the old and new evidence into a list so that both may exist
                case DuplicateEvidenceAction.Merge:

                    LegacyEvidenceList list = original as LegacyEvidenceList;
                    if (list == null)
                    {
                        list = new LegacyEvidenceList();
                        list.Add(original);
                    }

                    list.Add(duplicate);
                    return list;

                default:
                    BCLDebug.Assert(false, "Uknown DuplicateEvidenceAction");
                    return null;
            }
        }

        /// <summary>
        ///     Wrap evidence we recieved through a legacy API to ensure that it is stored in an EvidenceBase
        /// </summary>
        private static EvidenceBase WrapLegacyEvidence(object evidence)
        {
            Contract.Assert(evidence != null);

            EvidenceBase wrappedEvidence = evidence as EvidenceBase;
            if (wrappedEvidence == null)
            {
                wrappedEvidence = new LegacyEvidenceWrapper(evidence);
            }

            return wrappedEvidence;
        }

        /// <summary>
        ///     Upwrap evidence stored in a legacy adapter.
        ///     
        ///     This is only necessary for the case where multiple objects derived from EvidenceBase is
        ///     are added via the legacy APIs and are then retrieved via GetHostEvidence. This may occur if
        ///     a legacy application adds CLR supplied evidence types via the old APIs and a new application
        ///     consumes the resulting evidence.
        /// </summary>
        private static object UnwrapEvidence(EvidenceBase evidence)
        {
            ILegacyEvidenceAdapter adapter = evidence as ILegacyEvidenceAdapter;
            return adapter == null ? evidence : adapter.EvidenceObject;
        }

        /// <summary>
        ///     Merge two evidence collections together.  Note that this will cause all of the lazily
        ///     generated evidence for the input collection to be generated, as well as causing any lazily
        ///     generated evidence that both collections share to be generated in the target.
        /// </summary>
        [SecuritySafeCritical]
        public void Merge(Evidence evidence)
        {
            if (evidence == null)
            {
                return;
            }

            using (EvidenceLockHolder lockHolder = new EvidenceLockHolder(this, EvidenceLockHolder.LockType.Writer))
            {
                bool checkedLock = false;
                IEnumerator hostEnumerator = evidence.GetHostEnumerator();
                while (hostEnumerator.MoveNext())
                {
                    if (Locked && !checkedLock)
                    {
                        new SecurityPermission(SecurityPermissionFlag.ControlEvidence).Demand();
                        checkedLock = true;
                    }

                    // If we could potentially have evidence of the type about to be merged into our host list,
                    // then make sure that we generate that evidence before merging.  This will prevent the
                    // newly merged evidence from masking the value that we would have generated on our own.
                    Type hostEvidenceType = hostEnumerator.Current.GetType();
                    if (m_evidence.ContainsKey(hostEvidenceType))
                    {
                        GetHostEvidenceNoLock(hostEvidenceType);
                    }

                    EvidenceBase hostEvidence = WrapLegacyEvidence(hostEnumerator.Current);
                    AddHostEvidenceNoLock(hostEvidence,
                                          GetEvidenceIndexType(hostEvidence),
                                          DuplicateEvidenceAction.Merge);
                }

                // Add each piece of assembly evidence. We don't need to deserialize our copy of the
                // evidence because AddAssemblyEvidenceNoLock will do this for us.
                IEnumerator assemblyEnumerator = evidence.GetAssemblyEnumerator();
                while (assemblyEnumerator.MoveNext())
                {
                    EvidenceBase assemblyEvidence = WrapLegacyEvidence(assemblyEnumerator.Current);
                    AddAssemblyEvidenceNoLock(assemblyEvidence,
                                              GetEvidenceIndexType(assemblyEvidence),
                                              DuplicateEvidenceAction.Merge);
                }
            }
        }

        /// <summary>
        ///     Same as merge, except only one instance of any one evidence type is allowed. When duplicates
        ///     are found, the evidence in the input argument will have priority. Note this will force the
        ///     entire input evidence to be generated, and does not check for locked evidence
        /// </summary>
        internal void MergeWithNoDuplicates(Evidence evidence)
        {
            if (evidence == null)
            {
                return;
            }

            using (EvidenceLockHolder lockHolder = new EvidenceLockHolder(this, EvidenceLockHolder.LockType.Writer))
            {
                IEnumerator hostEnumerator = evidence.GetHostEnumerator();
                while (hostEnumerator.MoveNext())
                {
                    EvidenceBase hostEvidence = WrapLegacyEvidence(hostEnumerator.Current);
                    AddHostEvidenceNoLock(hostEvidence,
                                          GetEvidenceIndexType(hostEvidence),
                                          DuplicateEvidenceAction.SelectNewObject);
                }

                IEnumerator assemblyEnumerator = evidence.GetAssemblyEnumerator();
                while (assemblyEnumerator.MoveNext())
                {
                    EvidenceBase assemblyEvidence = WrapLegacyEvidence(assemblyEnumerator.Current);
                    AddAssemblyEvidenceNoLock(assemblyEvidence,
                                              GetEvidenceIndexType(assemblyEvidence),
                                              DuplicateEvidenceAction.SelectNewObject);
                }
            }
        }

#if FEATURE_SERIALIZATION
        /// <summary>
        ///     Do a full serialization of the evidence, which requires that we generate all of the evidence
        ///     we can and disconnect ourselves from the host and source assembly.
        /// </summary>
        [ComVisible(false)]
        [OnSerializing]
        [SecurityCritical]
        [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
        private void OnSerializing(StreamingContext context)
        {
            using (EvidenceLockHolder lockHolder = new EvidenceLockHolder(this, EvidenceLockHolder.LockType.Reader))
            {
                // First, force all of the host evidence that might be lazily generated to be created
                foreach (Type evidenceType in new List<Type>(m_evidence.Keys))
                {
                    GetHostEvidenceNoLock(evidenceType);
                }

                // Also ensure that all serialized assembly evidence has been created
                DeserializeTargetEvidence();
            }

            // Fill in legacy evidence lists. We can't guarantee thread-safety here using locks
            // because we can't put a lock in the serialization code that will read the lists.
            // The best we can do is prevent another thread from seeing a half-populated list.
            // Therefore, we assign the lists after we've populated them fully (and declare them volatile.)
            ArrayList hostList = new ArrayList();
            IEnumerator hostEnumerator = GetHostEnumerator();
            while (hostEnumerator.MoveNext())
            {
                hostList.Add(hostEnumerator.Current);
            }
            m_hostList = hostList;

            ArrayList assemblyList = new ArrayList();
            IEnumerator assemblyEnumerator = GetAssemblyEnumerator();
            while (assemblyEnumerator.MoveNext())
            {
                assemblyList.Add(assemblyEnumerator.Current);
            }
            m_assemblyList = assemblyList;
        }

        /// <summary>
        ///     Finish deserializing legacy evidence
        /// </summary>
        [ComVisible(false)]
        [OnDeserialized]
        [SecurityCritical]
        private void OnDeserialized(StreamingContext context)
        {
            // Look at host and assembly evidence lists only if we serialized using Whidbey.
            if (m_evidence == null)
            {
                m_evidence = new Dictionary<Type, EvidenceTypeDescriptor>();

                // Whidbey evidence may need to be wrapped or added to a LegacyEvidenceList, so we go
                // through the legacy APIs to add them.
#pragma warning disable 618
                if (m_hostList != null)
                {
                    foreach (object evidenceObject in m_hostList)
                    {
                        if (evidenceObject != null)
                        {
                            AddHost(evidenceObject);
                        }
                    }

                    m_hostList = null;
                }

                if (m_assemblyList != null)
                {
                    foreach (object evidenceObject in m_assemblyList)
                    {
                        if (evidenceObject != null)
                        {
                            AddAssembly(evidenceObject);
                        }
                    }

                    m_assemblyList = null;
                }
#pragma warning restore 618
            }

            // see code:System.Security.Policy.Evidence#EvidenceLock
            m_evidenceLock = new ReaderWriterLock();
        }
#endif // FEATURE_SERIALIZATION

        /// <summary>
        ///     Load any serialized evidence out of the target assembly into our evidence collection.
        ///     
        ///     We allow entry to this method with only a reader lock held, since most of the time we will
        ///     not need to write to the evidence dictionary. If we haven't yet deserialized the target
        ///     evidence, then we will upgrade to a writer lock at that point.
        /// </summary>
        private void DeserializeTargetEvidence()
        {
#if FEATURE_SERIALIZATION
            Contract.Assert(IsReaderLockHeld || IsWriterLockHeld);

            if (m_target != null && !m_deserializedTargetEvidence)
            {
                bool upgradedLock = false;
                LockCookie lockCookie = new LockCookie();
                try
                {
                    if (!IsWriterLockHeld)
                    {
                        lockCookie = UpgradeToWriterLock();
                        upgradedLock = true;
                    }

                    // Set this to true here because AddAssemblyEvidenceNoLock will attempt to reenter this
                    // method creating possible infinite recursion.
                    m_deserializedTargetEvidence = true;

                    foreach (EvidenceBase targetEvidence in m_target.GetFactorySuppliedEvidence())
                    {
                        AddAssemblyEvidenceNoLock(targetEvidence, GetEvidenceIndexType(targetEvidence), DuplicateEvidenceAction.Throw);
                    }
                }
                finally
                {
                    if (upgradedLock)
                        DowngradeFromWriterLock(ref lockCookie);
                }
            }
#endif // FEATURE_SERIALIZATION
        }

#if FEATURE_SERIALIZATION
        /// <summary>
        ///     Serialize out raw evidence objects which have already been generated, ignoring any evidence
        ///     which might be present but has not yet been created for this assembly.
        ///     
        ///     This is used for indexing into the security policy cache, since we know that once policy is
        ///     resolved, the relevent membership conditions will have checked for any applicable evidence
        ///     and therefore after poliyc resolution this evidence collection will contain any evidence
        ///     objects necessary to arrive at its grant set.
        /// </summary>
        [SecurityCritical]
        internal byte[] RawSerialize()
        {
            try
            {
                using (EvidenceLockHolder lockHolder = new EvidenceLockHolder(this, EvidenceLockHolder.LockType.Reader))
                {
                    // Filter out any evidence which is not yet generated
                    Dictionary<Type, EvidenceBase> generatedEvidence = new Dictionary<Type, EvidenceBase>();
                    foreach (KeyValuePair<Type, EvidenceTypeDescriptor> evidenceType in m_evidence)
                    {
                        if (evidenceType.Value != null && evidenceType.Value.HostEvidence != null)
                        {
                            generatedEvidence[evidenceType.Key] = evidenceType.Value.HostEvidence;
                        }
                    }

                    using (MemoryStream serializationStream = new MemoryStream())
                    {
                        BinaryFormatter formatter = new BinaryFormatter();
                        formatter.Serialize(serializationStream, generatedEvidence);
                        return serializationStream.ToArray();
                    }
                }
            }
            catch (SecurityException)
            {
                // We're running in a context where it's not safe to serialize the evidence out.  In this case
                // Simply decline to cache the result of the policy evaluation
                return null;
            }
        }
#endif // FEATURE_SERIALIZATION

        //
        // ICollection implementation.  All ICollection interface members are potentially much more
        // expensive in Arrowhead then they were downlevel.  They should not be used if the standard Get and
        // Add methods will work instead.
        // 

        [Obsolete("Evidence should not be treated as an ICollection. Please use the GetHostEnumerator and GetAssemblyEnumerator methods rather than using CopyTo.")]
        public void CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (index < 0 || index > array.Length - Count)
                throw new ArgumentOutOfRangeException("index");
            Contract.EndContractBlock();

            int currentIndex = index;

            IEnumerator hostEnumerator = GetHostEnumerator();
            while (hostEnumerator.MoveNext())
            {
                array.SetValue(hostEnumerator.Current, currentIndex);
                ++currentIndex;
            }

            IEnumerator assemblyEnumerator = GetAssemblyEnumerator();
            while (assemblyEnumerator.MoveNext())
            {
                array.SetValue(assemblyEnumerator.Current, currentIndex);
                ++currentIndex;
            }
        }

        public IEnumerator GetHostEnumerator()
        {
            using (EvidenceLockHolder lockHolder = new EvidenceLockHolder(this, EvidenceLockHolder.LockType.Reader))
            {
                return new EvidenceEnumerator(this, EvidenceEnumerator.Category.Host);
            }
        }

        public IEnumerator GetAssemblyEnumerator()
        {
            using (EvidenceLockHolder lockHolder = new EvidenceLockHolder(this, EvidenceLockHolder.LockType.Reader))
            {
                DeserializeTargetEvidence();
                return new EvidenceEnumerator(this, EvidenceEnumerator.Category.Assembly);
            }
        }

        /// <summary>
        ///     Get an enumerator that can iterate over the raw evidence objects stored for the assembly
        /// </summary>
        internal RawEvidenceEnumerator GetRawAssemblyEvidenceEnumerator()
        {
            Contract.Assert(IsReaderLockHeld);
            DeserializeTargetEvidence();
            return new RawEvidenceEnumerator(this, new List<Type>(m_evidence.Keys), false);
        }

        /// <summary>
        ///     Get an enumerator that can iterate over the raw evidence objects stored for the host
        /// </summary>
        /// <returns></returns>
        internal RawEvidenceEnumerator GetRawHostEvidenceEnumerator()
        {
            Contract.Assert(IsReaderLockHeld);
            return new RawEvidenceEnumerator(this, new List<Type>(m_evidence.Keys), true);
        }

        [Obsolete("GetEnumerator is obsolete. Please use GetAssemblyEnumerator and GetHostEnumerator instead.")]
        public IEnumerator GetEnumerator()
        {
            using (EvidenceLockHolder lockHolder = new EvidenceLockHolder(this, EvidenceLockHolder.LockType.Reader))
            {
                return new EvidenceEnumerator(this, EvidenceEnumerator.Category.Host | EvidenceEnumerator.Category.Assembly);
            }
        }

        /// <summary>
        ///     Get a specific type of assembly supplied evidence
        /// </summary>
        [ComVisible(false)]
        public T GetAssemblyEvidence<T>() where T : EvidenceBase
        {
            return UnwrapEvidence(GetAssemblyEvidence(typeof(T))) as T;
        }

        internal EvidenceBase GetAssemblyEvidence(Type type)
        {
            Contract.Assert(type != null);

            using (EvidenceLockHolder lockHolder = new EvidenceLockHolder(this, EvidenceLockHolder.LockType.Reader))
            {
                return GetAssemblyEvidenceNoLock(type);
            }
        }

        private EvidenceBase GetAssemblyEvidenceNoLock(Type type)
        {
            Contract.Assert(IsReaderLockHeld || IsWriterLockHeld);
            Contract.Assert(type != null);

            DeserializeTargetEvidence();
            EvidenceTypeDescriptor descriptor = GetEvidenceTypeDescriptor(type);
            if (descriptor != null)
            {
                return descriptor.AssemblyEvidence;
            }

            return null;
        }

        /// <summary>
        ///     Get a specific type of host supplied evidence
        /// </summary>
        [ComVisible(false)]
        public T GetHostEvidence<T>() where T : EvidenceBase
        {
            return UnwrapEvidence(GetHostEvidence(typeof(T))) as T;
        }

        /// <summary>
        ///     Get a specific type of evidence from the host which may not have been verified yet.  If the
        ///     evidence was not verified, then don't mark it as being used yet.
        /// </summary>
        internal T GetDelayEvaluatedHostEvidence<T>() where T : EvidenceBase, IDelayEvaluatedEvidence
        {
            return UnwrapEvidence(GetHostEvidence(typeof(T), false)) as T;
        }

        internal EvidenceBase GetHostEvidence(Type type)
        {
            Contract.Assert(type != null);

            return GetHostEvidence(type, true);
        }

        [SecuritySafeCritical]
        private EvidenceBase GetHostEvidence(Type type, bool markDelayEvaluatedEvidenceUsed)
        {
            Contract.Assert(type != null);

            using (EvidenceLockHolder lockHolder = new EvidenceLockHolder(this, EvidenceLockHolder.LockType.Reader))
            {
                EvidenceBase evidence = GetHostEvidenceNoLock(type);

                if (markDelayEvaluatedEvidenceUsed)
                {
                    IDelayEvaluatedEvidence delayEvidence = evidence as IDelayEvaluatedEvidence;
                    if (delayEvidence != null)
                    {
                        delayEvidence.MarkUsed();
                    }
                }

                return evidence;
            }
        }

        /// <summary>
        ///     Get host supplied evidence from the collection
        ///
        ///     We attempt to find host evdience in the following order:
        ///     
        ///       1. Already generated or explicitly supplied evidence
        ///       2. Evidence supplied by the CLR host
        ///       3. Evidence supplied by the CLR itself
        /// </summary>
        [SecurityCritical]
        private EvidenceBase GetHostEvidenceNoLock(Type type)
        {
            Contract.Assert(IsReaderLockHeld || IsWriterLockHeld);
            Contract.Assert(type != null);

            EvidenceTypeDescriptor descriptor = GetEvidenceTypeDescriptor(type);

            // If the evidence descriptor doesn't exist for the host evidence type than the evidence doesn't
            // exist and neither the host nor the runtime can produce it.
            if (descriptor == null)
            {
                return null;
            }

            // If the evidence has already been generated or if it was explicitly provided then return that
            if (descriptor.HostEvidence != null)
            {
                return descriptor.HostEvidence;
            }

            // If we have a target, then the host or the runtime might be able to generate this type of
            // evidence on demand.
            if (m_target != null && !descriptor.Generated)
            {
                using (EvidenceUpgradeLockHolder lockHolder = new EvidenceUpgradeLockHolder(this))
                {
                    // Make sure that we don't attempt to generate this type of evidencea again if we fail to
                    // generate it now.
                    descriptor.Generated = true;

                    EvidenceBase generatedEvidence = GenerateHostEvidence(type, descriptor.HostCanGenerate);
                    if (generatedEvidence != null)
                    {
                        descriptor.HostEvidence = generatedEvidence;

                        //
                        // #BackpatchGeneratedEvidence
                        // 
                        // If we were cloned from another evidence collection propigate any generated evidence
                        // back to the original collection.  Since Assembly and AppDomain both clone their
                        // evidence before giving it to users, this prevents us from having to regenerate
                        // evidence types on each clone that gets created.  Note that we do not want to do this
                        // backpatching if the origin already has evidence of this type or if it has had
                        // this type of evidence removed from its collection.
                        //

                        Evidence cloneOrigin = m_cloneOrigin != null ? m_cloneOrigin.Target as Evidence : null;
                        if (cloneOrigin != null)
                        {
                            BCLDebug.Assert(cloneOrigin.Target != null && cloneOrigin.Target == Target,
                                            "Attempt to backpatch evidence to a collection with a different target.");

                            using (EvidenceLockHolder cloneLockHolder = new EvidenceLockHolder(cloneOrigin, EvidenceLockHolder.LockType.Writer))
                            {
                                EvidenceTypeDescriptor cloneDescriptor = cloneOrigin.GetEvidenceTypeDescriptor(type);
                                if (cloneDescriptor != null && cloneDescriptor.HostEvidence == null)
                                {
                                    cloneDescriptor.HostEvidence = generatedEvidence.Clone() as EvidenceBase;
                                }
                            }
                        }

                    }

                    return generatedEvidence;
                }
            }

            // The evidence could not be generated and was not found
            return null;
        }

        /// <summary>
        ///     Attempt to generate host evidence on demand via calls to the runtime host or the evidence facotry
        /// </summary>
        [SecurityCritical]
        private EvidenceBase GenerateHostEvidence(Type type, bool hostCanGenerate)
        {
            Contract.Assert(type != null);
            Contract.Assert(IsWriterLockHeld);

#if FEATURE_CAS_POLICY
            // First let the host generate the evidence if it can.
            if (hostCanGenerate)
            {
                AppDomain targetDomain = m_target.Target as AppDomain;
                Assembly targetAssembly = m_target.Target as Assembly;

                EvidenceBase hostEvidence = null;
                if (targetDomain != null)
                {
                    hostEvidence = AppDomain.CurrentDomain.HostSecurityManager.GenerateAppDomainEvidence(type);
                }
                else if (targetAssembly != null)
                {
                    hostEvidence = AppDomain.CurrentDomain.HostSecurityManager.GenerateAssemblyEvidence(type, targetAssembly);
                }

                // If the host generated the evidence, verify that it generated the evidence we expected
                // and use that.
                if (hostEvidence != null)
                {
                    if (!type.IsAssignableFrom(hostEvidence.GetType()))
                    {
                        string hostType = AppDomain.CurrentDomain.HostSecurityManager.GetType().FullName;
                        string recievedType = hostEvidence.GetType().FullName;
                        string requestedType = type.FullName;

                        throw new InvalidOperationException(Environment.GetResourceString("Policy_IncorrectHostEvidence", hostType, recievedType, requestedType));
                    }

                    return hostEvidence;
                }
            }
#endif // FEATURE_CAS_POLICY

            // Finally, check to see if the CLR can generate the evidence
            return m_target.GenerateEvidence(type);
        }

        [Obsolete("Evidence should not be treated as an ICollection. Please use GetHostEnumerator and GetAssemblyEnumerator to iterate over the evidence to collect a count.")]
        public int Count
        {
            get
            {
                int count = 0;

                IEnumerator hostEvidence = GetHostEnumerator();
                while (hostEvidence.MoveNext())
                {
                    ++count;
                }

                IEnumerator assemblyEvidence = GetAssemblyEnumerator();
                while (assemblyEvidence.MoveNext())
                {
                    ++count;
                }

                return count;
            }
        }

        /// <summary>
        ///     Get the number of pieces of evidence which are currently generated, without causing any
        ///     lazily generated evidence to be created.
        /// </summary>
        [ComVisible(false)]
        internal int RawCount
        {
            get
            {
                int count = 0;

                using (EvidenceLockHolder lockHolder = new EvidenceLockHolder(this, EvidenceLockHolder.LockType.Reader))
                {
                    foreach (Type evidenceType in new List<Type>(m_evidence.Keys))
                    {
                        EvidenceTypeDescriptor descriptor = GetEvidenceTypeDescriptor(evidenceType);

                        if (descriptor != null)
                        {
                            if (descriptor.AssemblyEvidence != null)
                            {
                                ++count;
                            }
                            if (descriptor.HostEvidence != null)
                            {
                                ++count;
                            }
                        }
                    }
                }

                return count;
            }
        }

        public Object SyncRoot
        {
            get { return this; }
        }

        public bool IsSynchronized
        {
            get { return true; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

#if FEATURE_CAS_POLICY
        [ComVisible(false)]
        public Evidence Clone()
        {
            return new Evidence(this);
        }
#endif // FEATURE_CAS_POLICY

        [ComVisible(false)]
        [SecuritySafeCritical]
        public void Clear()
        {
            if (Locked)
            {
                new SecurityPermission(SecurityPermissionFlag.ControlEvidence).Demand();
            }

            using (EvidenceLockHolder lockHolder = new EvidenceLockHolder(this, EvidenceLockHolder.LockType.Writer))
            {
                ++m_version;
                m_evidence.Clear();
            }
        }

        [ComVisible(false)]
        [SecuritySafeCritical]
        public void RemoveType(Type t)
        {
            if (t == null)
                throw new ArgumentNullException("t");
            Contract.EndContractBlock();

            using (EvidenceLockHolder lockHolder = new EvidenceLockHolder(this, EvidenceLockHolder.LockType.Writer))
            {
                EvidenceTypeDescriptor descriptor = GetEvidenceTypeDescriptor(t);
                if (descriptor != null)
                {
                    ++m_version;

                    // If we've locked this evidence collection, we need to do the lock check in the case that 
                    // either we have host evidence, or that the host might generate it, since removing the
                    // evidence will cause us to bypass the host's ability to ever generate the evidence.
                    if (Locked && (descriptor.HostEvidence != null || descriptor.HostCanGenerate))
                    {
                        new SecurityPermission(SecurityPermissionFlag.ControlEvidence).Demand();
                    }

                    m_evidence.Remove(t);
                }
            }
        }

        /// <summary>
        ///     Mark all of the already generated evidence in the collection as having been used during a
        ///     policy evaluation.
        /// </summary>
        internal void MarkAllEvidenceAsUsed()
        {
            using (EvidenceLockHolder lockHolder = new EvidenceLockHolder(this, EvidenceLockHolder.LockType.Reader))
            {
                foreach (KeyValuePair<Type, EvidenceTypeDescriptor> evidenceType in m_evidence)
                {
                    if (evidenceType.Value != null)
                    {
                        IDelayEvaluatedEvidence hostEvidence = evidenceType.Value.HostEvidence as IDelayEvaluatedEvidence;
                        if (hostEvidence != null)
                        {
                            hostEvidence.MarkUsed();
                        }

                        IDelayEvaluatedEvidence assemblyEvidence = evidenceType.Value.AssemblyEvidence as IDelayEvaluatedEvidence;
                        if (assemblyEvidence != null)
                        {
                            assemblyEvidence.MarkUsed();
                        }
                    }
                }
            }
        }

#if FEATURE_CAS_POLICY
        /// <summary>
        ///     Determine if delay evaluated strong name evidence is contained in this collection, and if so
        ///     if it was used during policy evaluation.
        ///     
        ///     This method is called from the VM in SecurityPolicy::WasStrongNameEvidenceUsed
        ///     This class should be used as an adapter layer to allow the public facing EvidenceEnumerator to
        ///     be able to get the evidence values out of an Evidence class.  It is tightly coupled with the
        ///     internal data structures holding the evidence objects in the Evidence class.
        /// </summary>
        private bool WasStrongNameEvidenceUsed()
        {
            using (EvidenceLockHolder lockHolder = new EvidenceLockHolder(this, EvidenceLockHolder.LockType.Reader))
            {
                EvidenceTypeDescriptor snTypeDescriptor = GetEvidenceTypeDescriptor(typeof(StrongName));
                if (snTypeDescriptor != null)
                {
                    IDelayEvaluatedEvidence snEvidence = snTypeDescriptor.HostEvidence as IDelayEvaluatedEvidence;
                    return snEvidence != null && snEvidence.WasUsed;
                }

                return false;
            }
        }
#endif // FEATURE_CAS_POLICY

        /// <summary>
        ///     Utility class to wrap acquiring a lock onto the evidence collection
        /// </summary>
        private class EvidenceLockHolder : IDisposable
        {
            private Evidence m_target;
            private LockType m_lockType;

            public enum LockType
            {
                Reader,
                Writer
            }

            public EvidenceLockHolder(Evidence target, LockType lockType)
            {
                Contract.Assert(target != null);
                Contract.Assert(lockType == LockType.Reader || lockType == LockType.Writer);

                m_target = target;
                m_lockType = lockType;

                if (m_lockType == LockType.Reader)
                {
                    m_target.AcquireReaderLock();
                }
                else
                {
                    m_target.AcquireWriterlock();
                }
            }

            public void Dispose()
            {
                if (m_lockType == LockType.Reader && m_target.IsReaderLockHeld)
                {
                    m_target.ReleaseReaderLock();
                }
                else if (m_lockType == LockType.Writer && m_target.IsWriterLockHeld)
                {
                    m_target.ReleaseWriterLock();
                }
            }
        }

        /// <summary>
        ///     Utility class to wrap upgrading an acquired reader lock to a writer lock and then
        ///     downgrading it back to a reader lock.
        /// </summary>
        private class EvidenceUpgradeLockHolder : IDisposable
        {
            private Evidence m_target;
            private LockCookie m_cookie;

            public EvidenceUpgradeLockHolder(Evidence target)
            {
                Contract.Assert(target != null);

                m_target = target;
                m_cookie = m_target.UpgradeToWriterLock();
            }

            public void Dispose()
            {
                if (m_target.IsWriterLockHeld)
                {
                    m_target.DowngradeFromWriterLock(ref m_cookie);
                }
            }
        }

        /// <summary>
        ///     Enumerator that iterates directly over the evidence type map, returning back the evidence objects
        ///     that are contained in it.  This enumerator will generate any lazy evaluated evidence it finds,
        ///     but it does not attempt to deal with legacy evidence adapters.
        ///     
        ///     This class should be used as an adapter layer to allow the public facing EvidenceEnumerator to
        ///     be able to get the evidence values out of an Evidence class.  It is tightly coupled with the
        ///     internal data structures holding the evidence objects in the Evidence class.
        /// </summary>
        internal sealed class RawEvidenceEnumerator : IEnumerator<EvidenceBase>
        {
            private Evidence m_evidence;
            private bool m_hostEnumerator;      // true to enumerate host evidence, false to enumerate assembly evidence
            private uint m_evidenceVersion;

            private Type[] m_evidenceTypes;
            private int m_typeIndex;
            private EvidenceBase m_currentEvidence;

            private static volatile List<Type> s_expensiveEvidence;

            public RawEvidenceEnumerator(Evidence evidence, IEnumerable<Type> evidenceTypes, bool hostEnumerator)
            {
                Contract.Assert(evidence != null);
                Contract.Assert(evidenceTypes != null);

                m_evidence = evidence;
                m_hostEnumerator = hostEnumerator;
                m_evidenceTypes = GenerateEvidenceTypes(evidence, evidenceTypes, hostEnumerator);
                m_evidenceVersion = evidence.m_version;

                Reset();
            }

            public EvidenceBase Current
            {
                get
                {
                    if (m_evidence.m_version != m_evidenceVersion)
                        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumFailedVersion"));

                    return m_currentEvidence;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    if (m_evidence.m_version != m_evidenceVersion)
                        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumFailedVersion"));

                    return m_currentEvidence;
                }
            }

            /// <summary>
            ///     List of types of evidence that we would like to avoid generating if possible
            /// </summary>
            private static List<Type> ExpensiveEvidence
            {
                get
                {
                    if (s_expensiveEvidence == null)
                    {
                        List<Type> expensiveEvidence = new List<Type>();
#if FEATURE_CAS_POLICY
                        expensiveEvidence.Add(typeof(Hash));
                        expensiveEvidence.Add(typeof(Publisher));
#endif // FEATURE_CAS_POLICY
                        s_expensiveEvidence = expensiveEvidence;

#if _DEBUG
                        List<Type> runtimeTypes = new List<Type>(Evidence.RuntimeEvidenceTypes);
                        foreach (Type expensiveType in s_expensiveEvidence)
                        {
                            BCLDebug.Assert(runtimeTypes.Contains(expensiveType),
                                            "Evidence type not generated by the runtime found in expensive evidence type list");
                        }
#endif // _DEBUG
                    }

                    return s_expensiveEvidence;
                }
            }

            public void Dispose()
            {
                return;
            }

            /// <summary>
            ///     Generate the array of types of evidence that could have values for
            /// </summary>
            private static Type[] GenerateEvidenceTypes(Evidence evidence,
                                                        IEnumerable<Type> evidenceTypes,
                                                        bool hostEvidence)
            {
                Contract.Assert(evidence != null);
                Contract.Assert(evidenceTypes != null);

                //
                // Sort the evidence being generated into three categories, which we enumerate in order:
                //   1. Evidence which has already been generated
                //   2. Evidence which is relatively inexpensive to generate
                //   3. Evidence which is expensive to generate.
                //   
                // This allows us to be as efficient as possible in case the user of the enumerator stops the
                // enumeration before we step up to the next more expensive category.
                //

                List<Type> alreadyGeneratedList = new List<Type>();
                List<Type> inexpensiveList = new List<Type>();
                List<Type> expensiveList = new List<Type>(ExpensiveEvidence.Count);

                // Iterate over the evidence types classifying into the three groups.  We need to copy the list
                // here since GetEvidenceTypeDescriptor will potentially update the evidence dictionary, which
                // evidenceTypes iterates over.
                foreach (Type evidenceType in evidenceTypes)
                {
                    EvidenceTypeDescriptor descriptor = evidence.GetEvidenceTypeDescriptor(evidenceType);
                    BCLDebug.Assert(descriptor != null, "descriptor != null");

                    bool alreadyGenerated = (hostEvidence && descriptor.HostEvidence != null) ||
                                            (!hostEvidence && descriptor.AssemblyEvidence != null);

                    if (alreadyGenerated)
                    {
                        alreadyGeneratedList.Add(evidenceType);
                    }
                    else if (ExpensiveEvidence.Contains(evidenceType))
                    {
                        expensiveList.Add(evidenceType);
                    }
                    else
                    {
                        inexpensiveList.Add(evidenceType);
                    }
                }

                Type[] enumerationTypes = new Type[alreadyGeneratedList.Count + inexpensiveList.Count + expensiveList.Count];
                alreadyGeneratedList.CopyTo(enumerationTypes, 0);
                inexpensiveList.CopyTo(enumerationTypes, alreadyGeneratedList.Count);
                expensiveList.CopyTo(enumerationTypes, alreadyGeneratedList.Count + inexpensiveList.Count);

                return enumerationTypes;
            }

            [SecuritySafeCritical]
            public bool MoveNext()
            {
                using (EvidenceLockHolder lockHolder = new EvidenceLockHolder(m_evidence, EvidenceLockHolder.LockType.Reader))
                {
                    if (m_evidence.m_version != m_evidenceVersion)
                        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumFailedVersion"));

                    m_currentEvidence = null;

                    // Iterate over the possible types of evidence that we could have until we find one that
                    // really exists, or we run out of posibilities.
                    do
                    {
                        ++m_typeIndex;

                        if (m_typeIndex < m_evidenceTypes.Length)
                        {
                            if (m_hostEnumerator)
                            {
                                m_currentEvidence = m_evidence.GetHostEvidenceNoLock(m_evidenceTypes[m_typeIndex]);
                            }
                            else
                            {
                                m_currentEvidence = m_evidence.GetAssemblyEvidenceNoLock(m_evidenceTypes[m_typeIndex]);
                            }
                        }
                    }
                    while (m_typeIndex < m_evidenceTypes.Length && m_currentEvidence == null);
                }

                return m_currentEvidence != null;
            }

            public void Reset()
            {
                if (m_evidence.m_version != m_evidenceVersion)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumFailedVersion"));

                m_typeIndex = -1;
                m_currentEvidence = null;
            }
        }

        private sealed class EvidenceEnumerator : IEnumerator
        {
            private Evidence m_evidence;
            private Category m_category;
            private Stack m_enumerators;

            private object m_currentEvidence;

            [Flags]
            internal enum Category
            {
                Host = 0x1,     // Enumerate only host supplied evidence
                Assembly = 0x2      // Enumerate only assembly supplied evidence
            }

            internal EvidenceEnumerator(Evidence evidence, Category category)
            {
                Contract.Assert(evidence != null);
                Contract.Assert(evidence.IsReaderLockHeld);

                m_evidence = evidence;
                m_category = category;
                ResetNoLock();
            }

            public bool MoveNext()
            {
                IEnumerator currentEnumerator = CurrentEnumerator;

                // No more enumerators means we can't go any further
                if (currentEnumerator == null)
                {
                    m_currentEvidence = null;
                    return false;
                }

                // See if the current enumerator can continue
                if (currentEnumerator.MoveNext())
                {
                    //
                    // If we've found an adapter for legacy evidence, we need to unwrap it for it to be the
                    // current enumerator's value.  For wrapped evidence, this is a simple unwrap, for a list of
                    // evidence, we need to make that the current enumerator and get its first value.
                    // 

                    LegacyEvidenceWrapper legacyWrapper = currentEnumerator.Current as LegacyEvidenceWrapper;
                    LegacyEvidenceList legacyList = currentEnumerator.Current as LegacyEvidenceList;

                    if (legacyWrapper != null)
                    {
                        m_currentEvidence = legacyWrapper.EvidenceObject;
                    }
                    else if (legacyList != null)
                    {
                        IEnumerator legacyListEnumerator = legacyList.GetEnumerator();
                        m_enumerators.Push(legacyListEnumerator);
                        MoveNext();
                    }
                    else
                    {
                        m_currentEvidence = currentEnumerator.Current;
                    }

                    BCLDebug.Assert(m_currentEvidence != null, "m_currentEvidence != null");
                    return true;
                }
                else
                {
                    // If we've reached the end of the current enumerator, move to the next one and try again
                    m_enumerators.Pop();
                    return MoveNext();
                }
            }

            public object Current
            {
                get { return m_currentEvidence; }
            }

            private IEnumerator CurrentEnumerator
            {
                get
                {
                    return m_enumerators.Count > 0 ? m_enumerators.Peek() as IEnumerator : null;
                }
            }

            public void Reset()
            {
                using (EvidenceLockHolder lockHolder = new EvidenceLockHolder(m_evidence, EvidenceLockHolder.LockType.Reader))
                {
                    ResetNoLock();
                }
            }

            private void ResetNoLock()
            {
                Contract.Assert(m_evidence != null);
                Contract.Assert(m_evidence.IsReaderLockHeld);

                m_currentEvidence = null;
                m_enumerators = new Stack();

                if ((m_category & Category.Host) == Category.Host)
                {
                    m_enumerators.Push(m_evidence.GetRawHostEvidenceEnumerator());
                }
                if ((m_category & Category.Assembly) == Category.Assembly)
                {
                    m_enumerators.Push(m_evidence.GetRawAssemblyEvidenceEnumerator());
                }
            }
        }
#endif //!FEATURE_CORECLR && FEATURE_RWLOCK
    }
}
