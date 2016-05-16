// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

//
//
// This class encapsulates security decisions about an application.
//

namespace System.Security.Policy {
    using System.Collections;
    using System.Collections.Generic;
#if FEATURE_CLICKONCE        
    using System.Deployment.Internal.Isolation;
    using System.Deployment.Internal.Isolation.Manifest;
#endif    
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
#if FEATURE_SERIALIZATION
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;
#endif // FEATURE_SERIALIZATION
    using System.Runtime.Versioning;
    using System.Security.Permissions;
    using System.Security.Util;
    using System.Text;
    using System.Threading;
    using System.Diagnostics.Contracts;

    [System.Runtime.InteropServices.ComVisible(true)]
    public enum ApplicationVersionMatch {
        MatchExactVersion,
        MatchAllVersions
    }

    [System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    public sealed class ApplicationTrust : EvidenceBase, ISecurityEncodable
    {
#if FEATURE_CLICKONCE
        private ApplicationIdentity m_appId;
        private bool m_appTrustedToRun;
        private bool m_persist;
        
        private object m_extraInfo;
        private SecurityElement m_elExtraInfo;
#endif
        private PolicyStatement m_psDefaultGrant;
        private IList<StrongName> m_fullTrustAssemblies;

        // Permission special flags for the default grant set in this ApplicationTrust.  This should be
        // updated in sync with any updates to the default grant set.
        // 
        // In the general case, these values cannot be trusted - we only store a reference to the
        // DefaultGrantSet, and return the reference directly, which means that code can update the
        // permission set without our knowledge.  That would lead to the flags getting out of sync with the
        // grant set.
        // 
        // However, we only care about these flags when we're creating a homogenous AppDomain, and in that
        // case we control the ApplicationTrust object end-to-end, and know that the permission set will not
        // change after the flags are calculated.
        [NonSerialized]
        private int m_grantSetSpecialFlags;

#if FEATURE_CLICKONCE        
        public ApplicationTrust (ApplicationIdentity applicationIdentity) : this () {
            ApplicationIdentity = applicationIdentity;
        }
#endif 
        public ApplicationTrust () : this (new PermissionSet(PermissionState.None))
        {
        }

        internal ApplicationTrust (PermissionSet defaultGrantSet)
        {
            InitDefaultGrantSet(defaultGrantSet);

            m_fullTrustAssemblies = new List<StrongName>().AsReadOnly();
        }

        public ApplicationTrust(PermissionSet defaultGrantSet, IEnumerable<StrongName> fullTrustAssemblies) {
            if (fullTrustAssemblies == null) {
                throw new ArgumentNullException("fullTrustAssemblies");
            }

            InitDefaultGrantSet(defaultGrantSet);

            List<StrongName> fullTrustList = new List<StrongName>();
            foreach (StrongName strongName in fullTrustAssemblies) {
                if (strongName == null) {
                    throw new ArgumentException(Environment.GetResourceString("Argument_NullFullTrustAssembly"), "fullTrustAssemblies");
                }

                fullTrustList.Add(new StrongName(strongName.PublicKey, strongName.Name, strongName.Version));
            }

            m_fullTrustAssemblies = fullTrustList.AsReadOnly();
        }

        // Sets up the default grant set for all constructors. Extracted to avoid the cost of
        // IEnumerable virtual dispatches on startup when there are no fullTrustAssemblies (CoreCLR)
        private void InitDefaultGrantSet(PermissionSet defaultGrantSet) {
            if (defaultGrantSet == null) {
                throw new ArgumentNullException("defaultGrantSet");
            }

            // Creating a PolicyStatement copies the incoming permission set, so we don't have to worry
            // about the PermissionSet parameter changing underneath us after we've calculated the
            // permisison flags in the DefaultGrantSet setter.
            DefaultGrantSet = new PolicyStatement(defaultGrantSet);
        }

#if FEATURE_CLICKONCE        
        public ApplicationIdentity ApplicationIdentity {
            get {
                return m_appId;
            }
            set {
                if (value == null)
                    throw new ArgumentNullException("value", Environment.GetResourceString("Argument_InvalidAppId"));
                Contract.EndContractBlock();
                m_appId = value;
            }
        }
#endif
        public PolicyStatement DefaultGrantSet {
            get {
                if (m_psDefaultGrant == null)
                    return new PolicyStatement(new PermissionSet(PermissionState.None));
                return m_psDefaultGrant;
            }
            set {
                if (value == null) {
                    m_psDefaultGrant = null;
                    m_grantSetSpecialFlags = 0;
                }
                else {
                    m_psDefaultGrant = value;
                    m_grantSetSpecialFlags = SecurityManager.GetSpecialFlags(m_psDefaultGrant.PermissionSet, null);
                }
            }
        }

        public IList<StrongName> FullTrustAssemblies {
            get {
                return m_fullTrustAssemblies;
            }
        }
#if FEATURE_CLICKONCE        
        public bool IsApplicationTrustedToRun {
            get {
                return m_appTrustedToRun;
            }
            set {
                m_appTrustedToRun = value;
            }
        }

        public bool Persist {
            get {
                return m_persist;
            }
            set {
                m_persist = value;
            }
        }

         public object ExtraInfo {
            get {
                if (m_elExtraInfo != null) {
                    m_extraInfo = ObjectFromXml(m_elExtraInfo);
                    m_elExtraInfo = null;
                }
                return m_extraInfo;
            }
            set {
                m_elExtraInfo = null;
                m_extraInfo = value;
            }
        }
#endif //FEATURE_CLICKONCE

#if FEATURE_CAS_POLICY
        public SecurityElement ToXml () {
            SecurityElement elRoot = new SecurityElement("ApplicationTrust");
            elRoot.AddAttribute("version", "1");

#if FEATURE_CLICKONCE
            if (m_appId != null) {
                elRoot.AddAttribute("FullName", SecurityElement.Escape(m_appId.FullName));
            }
            if (m_appTrustedToRun) {
                elRoot.AddAttribute("TrustedToRun", "true");
            }
            if (m_persist) {
                elRoot.AddAttribute("Persist", "true");
            }
#endif // FEATURE_CLICKONCE
 
            if (m_psDefaultGrant != null) {
                SecurityElement elDefaultGrant = new SecurityElement("DefaultGrant");
                elDefaultGrant.AddChild(m_psDefaultGrant.ToXml());
                elRoot.AddChild(elDefaultGrant);
            }
            if (m_fullTrustAssemblies.Count > 0) {
                SecurityElement elFullTrustAssemblies = new SecurityElement("FullTrustAssemblies");
                foreach (StrongName fullTrustAssembly in m_fullTrustAssemblies) {
                    elFullTrustAssemblies.AddChild(fullTrustAssembly.ToXml());
                }
                elRoot.AddChild(elFullTrustAssemblies);
            }

#if FEATURE_CLICKONCE
            if (ExtraInfo != null) {
                elRoot.AddChild(ObjectToXml("ExtraInfo", ExtraInfo));
            }
#endif // FEATURE_CLICKONCE
            return elRoot;
        }

        public void FromXml (SecurityElement element) {
            if (element == null)
                throw new ArgumentNullException("element");
            if (String.Compare(element.Tag, "ApplicationTrust", StringComparison.Ordinal) != 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidXML"));

#if FEATURE_CLICKONCE
            m_appTrustedToRun = false;
            string isAppTrustedToRun = element.Attribute("TrustedToRun");
            if (isAppTrustedToRun != null && String.Compare(isAppTrustedToRun, "true", StringComparison.Ordinal) == 0) {
                m_appTrustedToRun = true;
            }

            m_persist = false;
            string persist = element.Attribute("Persist");
            if (persist != null && String.Compare(persist, "true", StringComparison.Ordinal) == 0) {
                m_persist = true;
            }

            m_appId = null;
            string fullName = element.Attribute("FullName");
            if (fullName != null && fullName.Length > 0) {
                m_appId = new ApplicationIdentity(fullName);
            }
#endif // FEATURE_CLICKONCE

            m_psDefaultGrant = null;
            m_grantSetSpecialFlags = 0;
            SecurityElement elDefaultGrant = element.SearchForChildByTag("DefaultGrant");
            if (elDefaultGrant != null) {
                SecurityElement elDefaultGrantPS = elDefaultGrant.SearchForChildByTag("PolicyStatement");
                if (elDefaultGrantPS != null) {
                    PolicyStatement ps = new PolicyStatement(null);
                    ps.FromXml(elDefaultGrantPS);
                    m_psDefaultGrant = ps;
                    m_grantSetSpecialFlags = SecurityManager.GetSpecialFlags(ps.PermissionSet, null);
                }
            }

            List<StrongName> fullTrustAssemblies = new List<StrongName>();
            SecurityElement elFullTrustAssemblies = element.SearchForChildByTag("FullTrustAssemblies");
            if (elFullTrustAssemblies != null && elFullTrustAssemblies.InternalChildren != null) {
                IEnumerator enumerator = elFullTrustAssemblies.Children.GetEnumerator();
                while (enumerator.MoveNext()) {
                    StrongName fullTrustAssembly = new StrongName();
                    fullTrustAssembly.FromXml(enumerator.Current as SecurityElement);
                    fullTrustAssemblies.Add(fullTrustAssembly);
                }
            }

            m_fullTrustAssemblies = fullTrustAssemblies.AsReadOnly();

#if FEATURE_CLICKONCE
            m_elExtraInfo = element.SearchForChildByTag("ExtraInfo");
#endif // FEATURE_CLICKONCE
        }

#if FEATURE_CLICKONCE
        private static SecurityElement ObjectToXml (string tag, Object obj) {
            BCLDebug.Assert(obj != null, "You need to pass in an object");

            ISecurityEncodable encodableObj = obj as ISecurityEncodable;

            SecurityElement elObject;
            if (encodableObj != null) {
                elObject = encodableObj.ToXml();
                if (!elObject.Tag.Equals(tag))
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidXML"));
            }

            MemoryStream stream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, obj);
            byte[] array = stream.ToArray();

            elObject = new SecurityElement(tag);
            elObject.AddAttribute("Data", Hex.EncodeHexString(array));
            return elObject;
        }

        private static Object ObjectFromXml (SecurityElement elObject) {
            BCLDebug.Assert(elObject != null, "You need to pass in a security element");

            if (elObject.Attribute("class") != null) {
                ISecurityEncodable encodableObj = XMLUtil.CreateCodeGroup(elObject) as ISecurityEncodable;
                if (encodableObj != null) {
                    encodableObj.FromXml(elObject);
                    return encodableObj;
                }
            }

            string objectData = elObject.Attribute("Data");
            MemoryStream stream = new MemoryStream(Hex.DecodeHexString(objectData));
            BinaryFormatter formatter = new BinaryFormatter();
            return formatter.Deserialize(stream);
        }
#endif // FEATURE_CLICKONCE
#endif // FEATURE_CAS_POLICY

#pragma warning disable 618
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
#pragma warning restore 618
        [SecuritySafeCritical]
        public override EvidenceBase Clone()
        {
            return base.Clone();
        }
    }

#if FEATURE_CLICKONCE
    [System.Security.SecurityCritical]  // auto-generated_required
    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class ApplicationTrustCollection : ICollection {
        private const string ApplicationTrustProperty = "ApplicationTrust";
        private const string InstallerIdentifier = "{60051b8f-4f12-400a-8e50-dd05ebd438d1}";
        private static Guid ClrPropertySet = new Guid("c989bb7a-8385-4715-98cf-a741a8edb823");

        // The CLR specific constant install reference.
        private static object s_installReference = null;
        private static StoreApplicationReference InstallReference {
            get {
                if (s_installReference == null) {
                    Interlocked.CompareExchange(ref s_installReference,
                                                new StoreApplicationReference(
                                                    IsolationInterop.GUID_SXS_INSTALL_REFERENCE_SCHEME_OPAQUESTRING,
                                                    InstallerIdentifier,
                                                    null),
                                                null);
                }
                return (StoreApplicationReference) s_installReference;
            }
        }

        private object m_appTrusts = null;
        private ArrayList AppTrusts {
            [System.Security.SecurityCritical]  // auto-generated
            get {
                if (m_appTrusts == null) {
                    ArrayList appTrusts = new ArrayList();
                    if (m_storeBounded) {
                        RefreshStorePointer();
                        // enumerate the user store and populate the collection
                        StoreDeploymentMetadataEnumeration deplEnum = m_pStore.EnumInstallerDeployments(IsolationInterop.GUID_SXS_INSTALL_REFERENCE_SCHEME_OPAQUESTRING, InstallerIdentifier, ApplicationTrustProperty, null);
                        foreach (IDefinitionAppId defAppId in deplEnum) {
                            StoreDeploymentMetadataPropertyEnumeration metadataEnum = m_pStore.EnumInstallerDeploymentProperties(IsolationInterop.GUID_SXS_INSTALL_REFERENCE_SCHEME_OPAQUESTRING, InstallerIdentifier, ApplicationTrustProperty, defAppId);
                            foreach (StoreOperationMetadataProperty appTrustProperty in metadataEnum) {
                                string appTrustXml = appTrustProperty.Value;
                                if (appTrustXml != null && appTrustXml.Length > 0) {
                                    SecurityElement seTrust = SecurityElement.FromString(appTrustXml);
                                    ApplicationTrust appTrust = new ApplicationTrust();
                                    appTrust.FromXml(seTrust);
                                    appTrusts.Add(appTrust);
                                }
                            }
                        }
                    }
                    Interlocked.CompareExchange(ref m_appTrusts, appTrusts, null);
                }
                return m_appTrusts as ArrayList;
            }
        }

        private bool m_storeBounded = false;
        private Store m_pStore = null; // Component store interface pointer.

        // Only internal constructors are exposed.
        [System.Security.SecurityCritical]  // auto-generated
        internal ApplicationTrustCollection () : this(false) {}
        internal ApplicationTrustCollection (bool storeBounded) {
            m_storeBounded = storeBounded;
        }

        [System.Security.SecurityCritical]  // auto-generated
        private void RefreshStorePointer () {
            // Refresh store pointer.
            if (m_pStore != null)
                Marshal.ReleaseComObject(m_pStore.InternalStore);
            m_pStore = IsolationInterop.GetUserStore();
        }

        public int Count
        {
            [System.Security.SecuritySafeCritical] // overrides public transparent member
            get {
                return AppTrusts.Count;
            }
        }

        public ApplicationTrust this[int index] {
            [System.Security.SecurityCritical]  // auto-generated
            get {
                return AppTrusts[index] as ApplicationTrust;
            }
        }

        public ApplicationTrust this[string appFullName] {
            [System.Security.SecurityCritical]  // auto-generated
            get {
                ApplicationIdentity identity = new ApplicationIdentity(appFullName);
                ApplicationTrustCollection appTrusts = Find(identity, ApplicationVersionMatch.MatchExactVersion);
                if (appTrusts.Count > 0)
                    return appTrusts[0];
                return null;
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        private void CommitApplicationTrust(ApplicationIdentity applicationIdentity, string trustXml) {
            StoreOperationMetadataProperty[] properties = new StoreOperationMetadataProperty[] {
                    new StoreOperationMetadataProperty(ClrPropertySet, ApplicationTrustProperty, trustXml)
                };

            IEnumDefinitionIdentity idenum = applicationIdentity.Identity.EnumAppPath();
            IDefinitionIdentity[] asbId = new IDefinitionIdentity[1];
            IDefinitionIdentity deplId = null;
            if (idenum.Next(1, asbId) == 1)
                deplId = asbId[0];

            IDefinitionAppId defAppId = IsolationInterop.AppIdAuthority.CreateDefinition();
            defAppId.SetAppPath(1, new IDefinitionIdentity[] {deplId});
            defAppId.put_Codebase(applicationIdentity.CodeBase);

            using (StoreTransaction storeTxn = new StoreTransaction()) {
                storeTxn.Add(new StoreOperationSetDeploymentMetadata(defAppId, InstallReference, properties));
                RefreshStorePointer();
                m_pStore.Transact(storeTxn.Operations);
            }

            m_appTrusts = null; // reset the app trusts in the collection.
        }

        [System.Security.SecurityCritical]  // auto-generated
        public int Add (ApplicationTrust trust) {
            if (trust == null)
                throw new ArgumentNullException("trust");
            if (trust.ApplicationIdentity == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_ApplicationTrustShouldHaveIdentity"));
            Contract.EndContractBlock();

            // Add the trust decision of the application to the fusion store.
            if (m_storeBounded) {
                CommitApplicationTrust(trust.ApplicationIdentity, trust.ToXml().ToString());
                return -1;
            } else {
                return AppTrusts.Add(trust);
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        public void AddRange (ApplicationTrust[] trusts) {
            if (trusts == null)
                throw new ArgumentNullException("trusts");
            Contract.EndContractBlock();

            int i=0;
            try {
                for (; i<trusts.Length; i++) {
                    Add(trusts[i]);
                }
            } catch {
                for (int j=0; j<i; j++) {
                    Remove(trusts[j]);
                }
                throw;
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        public void AddRange (ApplicationTrustCollection trusts) {
            if (trusts == null)
                throw new ArgumentNullException("trusts");
            Contract.EndContractBlock();

            int i = 0;
            try {
                foreach (ApplicationTrust trust in trusts) {
                    Add(trust);
                    i++;
                }
            } catch {
                for (int j=0; j<i; j++) {
                    Remove(trusts[j]);
                }
                throw;
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        public ApplicationTrustCollection Find (ApplicationIdentity applicationIdentity, ApplicationVersionMatch versionMatch) {
            ApplicationTrustCollection collection = new ApplicationTrustCollection(false);
            foreach (ApplicationTrust trust in this) {
                if (CmsUtils.CompareIdentities(trust.ApplicationIdentity, applicationIdentity, versionMatch))
                    collection.Add(trust);
            }
            return collection;
        }

        [System.Security.SecurityCritical]  // auto-generated
        public void Remove (ApplicationIdentity applicationIdentity, ApplicationVersionMatch versionMatch) {
            ApplicationTrustCollection collection = Find(applicationIdentity, versionMatch);
            RemoveRange(collection);
        }

        [System.Security.SecurityCritical]  // auto-generated
        public void Remove (ApplicationTrust trust) {
            if (trust == null)
                throw new ArgumentNullException("trust");
            if (trust.ApplicationIdentity == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_ApplicationTrustShouldHaveIdentity"));
            Contract.EndContractBlock();

            // Remove the trust decision of the application from the fusion store.
            if (m_storeBounded) {
                CommitApplicationTrust(trust.ApplicationIdentity, null);
            } else {
                AppTrusts.Remove(trust);
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        public void RemoveRange (ApplicationTrust[] trusts) {
            if (trusts == null)
                throw new ArgumentNullException("trusts");
            Contract.EndContractBlock();

            int i=0;
            try {
                for (; i<trusts.Length; i++) {
                    Remove(trusts[i]);
                }
            } catch {
                for (int j=0; j<i; j++) {
                    Add(trusts[j]);
                }
                throw;
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        public void RemoveRange (ApplicationTrustCollection trusts) {
            if (trusts == null)
                throw new ArgumentNullException("trusts");
            Contract.EndContractBlock();

            int i = 0;
            try {
                foreach (ApplicationTrust trust in trusts) {
                    Remove(trust);
                    i++;
                }
            } catch {
                for (int j=0; j<i; j++) {
                    Add(trusts[j]);
                }
                throw;
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        public void Clear() {
            // remove all trust decisions in the collection.
            ArrayList trusts = this.AppTrusts;
            if (m_storeBounded) {
                foreach (ApplicationTrust trust in trusts) {
                    if (trust.ApplicationIdentity == null)
                        throw new ArgumentException(Environment.GetResourceString("Argument_ApplicationTrustShouldHaveIdentity"));

                    // Remove the trust decision of the application from the fusion store.
                    CommitApplicationTrust(trust.ApplicationIdentity, null);
                }
            }
            trusts.Clear();
        }

        public ApplicationTrustEnumerator GetEnumerator() {
            return new ApplicationTrustEnumerator(this);
        }

        /// <internalonly/>
        [System.Security.SecuritySafeCritical] // overrides public transparent member
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new ApplicationTrustEnumerator(this);
        }

        /// <internalonly/>
        [System.Security.SecuritySafeCritical] // overrides public transparent member
        void ICollection.CopyTo(Array array, int index) {
            if (array == null)
                throw new ArgumentNullException("array");
            if (array.Rank != 1)
                throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
            if (index < 0 || index >= array.Length)
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            if (array.Length - index < this.Count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();

            for (int i=0; i < this.Count; i++) {
                array.SetValue(this[i], index++);
            }
        }

        public void CopyTo (ApplicationTrust[] array, int index) {
            ((ICollection)this).CopyTo(array, index);
        }

        public bool IsSynchronized {
            [System.Security.SecuritySafeCritical] // overrides public transparent member
            get
            {
                return false;
            }
        }

        public object SyncRoot {
            [System.Security.SecuritySafeCritical] // overrides public transparent member
            get
            {
                return this;
            }
        }
    }

    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class ApplicationTrustEnumerator : IEnumerator {
        [System.Security.SecurityCritical] // auto-generated
        private ApplicationTrustCollection m_trusts;
        private int m_current;

        private ApplicationTrustEnumerator() {}
        [System.Security.SecurityCritical]  // auto-generated
        internal ApplicationTrustEnumerator(ApplicationTrustCollection trusts) {
            m_trusts = trusts;
            m_current = -1;
        }

        public ApplicationTrust Current {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                return m_trusts[m_current];
            }
        }

        /// <internalonly/>
        object IEnumerator.Current {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                return (object) m_trusts[m_current];
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public bool MoveNext() {
            if (m_current == ((int) m_trusts.Count - 1))
                return false;
            m_current++;
            return true;
        }

        public void Reset() {
            m_current = -1;
        }
    }
#endif // FEATURE_CLICKONCE    
}
