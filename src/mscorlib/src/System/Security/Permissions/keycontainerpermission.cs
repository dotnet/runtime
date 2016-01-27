// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions {
    using System;
    using System.Collections;
    using System.Collections.Generic;
#if FEATURE_CRYPTO
    using System.Security.Cryptography;
#endif
    using System.Security.Util;
    using System.Globalization;
    using System.Diagnostics.Contracts;

[Serializable]
    [Flags]
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum KeyContainerPermissionFlags {
        NoFlags     = 0x0000,

        Create      = 0x0001,
        Open        = 0x0002,
        Delete      = 0x0004,

        Import      = 0x0010,
        Export      = 0x0020,

        Sign        = 0x0100,
        Decrypt     = 0x0200,

        ViewAcl     = 0x1000,
        ChangeAcl   = 0x2000,

        AllFlags    = 0x3337
    }

    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class KeyContainerPermissionAccessEntry {
        private string m_keyStore;
        private string m_providerName;
        private int m_providerType;
        private string m_keyContainerName;
        private int m_keySpec;
        private KeyContainerPermissionFlags m_flags;

        internal KeyContainerPermissionAccessEntry(KeyContainerPermissionAccessEntry accessEntry) : 
            this (accessEntry.KeyStore, accessEntry.ProviderName, accessEntry.ProviderType, accessEntry.KeyContainerName,
                  accessEntry.KeySpec, accessEntry.Flags) {
        }

        public KeyContainerPermissionAccessEntry(string keyContainerName, KeyContainerPermissionFlags flags) : 
            this (null, null, -1, keyContainerName, -1, flags) {
        }

#if FEATURE_CRYPTO
        public KeyContainerPermissionAccessEntry(CspParameters parameters, KeyContainerPermissionFlags flags) :
            this((parameters.Flags & CspProviderFlags.UseMachineKeyStore) == CspProviderFlags.UseMachineKeyStore ? "Machine" : "User",
                 parameters.ProviderName,
                 parameters.ProviderType,
                 parameters.KeyContainerName,
                 parameters.KeyNumber,
                 flags) {
        }
#endif

        public KeyContainerPermissionAccessEntry(string keyStore, string providerName, int providerType, 
                        string keyContainerName, int keySpec, KeyContainerPermissionFlags flags) {
            m_providerName = (providerName == null ? "*" : providerName);
            m_providerType = providerType;
            m_keyContainerName = (keyContainerName == null ? "*" : keyContainerName);
            m_keySpec = keySpec;
            KeyStore = keyStore;
            Flags = flags;
        }

        public string KeyStore {
            get {
                return m_keyStore;
            }
            set {
                // Unrestricted entries are invalid; they should not be allowed.
                if (IsUnrestrictedEntry(value, this.ProviderName, this.ProviderType, this.KeyContainerName, this.KeySpec))
                    throw new ArgumentException(Environment.GetResourceString("Arg_InvalidAccessEntry"));

                if (value == null) {
                    m_keyStore = "*";
                } else {
                    if (value != "User" && value != "Machine" && value != "*")
                        throw new ArgumentException(Environment.GetResourceString("Argument_InvalidKeyStore", value), "value");
                    m_keyStore = value;
                }
            }
        }

        public string ProviderName {
            get {
                return m_providerName;
            }
            set {
                // Unrestricted entries are invalid; they should not be allowed.
                if (IsUnrestrictedEntry(this.KeyStore, value, this.ProviderType, this.KeyContainerName, this.KeySpec))
                    throw new ArgumentException(Environment.GetResourceString("Arg_InvalidAccessEntry"));

                if (value == null)
                    m_providerName = "*";
                else
                    m_providerName = value;
            }
        }

        public int ProviderType {
            get {
                return m_providerType;
            }
            set {
                // Unrestricted entries are invalid; they should not be allowed.
                if (IsUnrestrictedEntry(this.KeyStore, this.ProviderName, value, this.KeyContainerName, this.KeySpec))
                    throw new ArgumentException(Environment.GetResourceString("Arg_InvalidAccessEntry"));

                m_providerType = value;
            }
        }

        public string KeyContainerName {
            get {
                return m_keyContainerName;
            }
            set {
                // Unrestricted entries are invalid; they should not be allowed.
                if (IsUnrestrictedEntry(this.KeyStore, this.ProviderName, this.ProviderType, value, this.KeySpec))
                    throw new ArgumentException(Environment.GetResourceString("Arg_InvalidAccessEntry"));

                if (value == null)
                    m_keyContainerName = "*";
                else
                    m_keyContainerName = value;
            }
        }

        public int KeySpec {
            get {
                return m_keySpec;
            }
            set {
                // Unrestricted entries are invalid; they should not be allowed.
                if (IsUnrestrictedEntry(this.KeyStore, this.ProviderName, this.ProviderType, this.KeyContainerName, value))
                    throw new ArgumentException(Environment.GetResourceString("Arg_InvalidAccessEntry"));

                m_keySpec = value;
            }
        }

        public KeyContainerPermissionFlags Flags {
            get {
                return m_flags;
            }
            set {
                KeyContainerPermission.VerifyFlags(value);
                m_flags = value;
            }
        }

        public override bool Equals (Object o) {
            KeyContainerPermissionAccessEntry accessEntry = o as KeyContainerPermissionAccessEntry;
            if (accessEntry == null)
                return false;

            if (accessEntry.m_keyStore != m_keyStore) return false;
            if (accessEntry.m_providerName != m_providerName) return false;
            if (accessEntry.m_providerType != m_providerType) return false;
            if (accessEntry.m_keyContainerName != m_keyContainerName) return false;
            if (accessEntry.m_keySpec != m_keySpec) return false;

            return true;
        }

        public override int GetHashCode () {
            int hash = 0;

            hash |= (this.m_keyStore.GetHashCode() & 0x000000FF) << 24;
            hash |= (this.m_providerName.GetHashCode() & 0x000000FF) << 16;
            hash |= (this.m_providerType & 0x0000000F) << 12;
            hash |= (this.m_keyContainerName.GetHashCode() & 0x000000FF) << 4;
            hash |= (this.m_keySpec & 0x0000000F);

            return hash;
        }

        internal bool IsSubsetOf (KeyContainerPermissionAccessEntry target) {
            if (target.m_keyStore != "*" && this.m_keyStore != target.m_keyStore)
                return false;
            if (target.m_providerName != "*" && this.m_providerName != target.m_providerName)
                return false;
            if (target.m_providerType != -1 && this.m_providerType != target.m_providerType)
                return false;
            if (target.m_keyContainerName != "*" && this.m_keyContainerName != target.m_keyContainerName)
                return false;
            if (target.m_keySpec != -1 && this.m_keySpec != target.m_keySpec)
                return false;

            return true;
        }

        internal static bool IsUnrestrictedEntry (string keyStore, string providerName, int providerType, 
                        string keyContainerName, int keySpec) {
            if (keyStore != "*" && keyStore != null) return false;
            if (providerName != "*" && providerName != null) return false;
            if (providerType != -1) return false;
            if (keyContainerName != "*" && keyContainerName != null) return false;
            if (keySpec != -1) return false;

            return true;
        }
    }

    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class KeyContainerPermissionAccessEntryCollection : ICollection {
        private ArrayList m_list;
        private KeyContainerPermissionFlags m_globalFlags;

        private KeyContainerPermissionAccessEntryCollection () {}
        internal KeyContainerPermissionAccessEntryCollection (KeyContainerPermissionFlags globalFlags) {
            m_list = new ArrayList();
            m_globalFlags = globalFlags;
        }

        public KeyContainerPermissionAccessEntry this[int index] {
            get {
                if (index < 0)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumNotStarted"));
                if (index >= Count)
                    throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                Contract.EndContractBlock();

                return (KeyContainerPermissionAccessEntry)m_list[index];
            }
        }

        public int Count {
            get {
                return m_list.Count;
            }
        }

        public int Add (KeyContainerPermissionAccessEntry accessEntry) {
            if (accessEntry == null)
                throw new ArgumentNullException("accessEntry");
            Contract.EndContractBlock();

            int index = m_list.IndexOf(accessEntry);
            if (index == -1) {
                if (accessEntry.Flags != m_globalFlags) {
                    return m_list.Add(accessEntry);
                }
                else
                    return -1;
            } else {
                // We pick up the intersection of the 2 flags. This is the secure choice 
                // so we are opting for it.
                ((KeyContainerPermissionAccessEntry)m_list[index]).Flags &= accessEntry.Flags;
                return index;
            }
        }

        public void Clear () {
            m_list.Clear();
        }

        public int IndexOf (KeyContainerPermissionAccessEntry accessEntry) {
            return m_list.IndexOf(accessEntry);
        }

        public void Remove (KeyContainerPermissionAccessEntry accessEntry) {
            if (accessEntry == null)
                throw new ArgumentNullException("accessEntry");
            Contract.EndContractBlock();
            m_list.Remove(accessEntry);
        }

        public KeyContainerPermissionAccessEntryEnumerator GetEnumerator () {
            return new KeyContainerPermissionAccessEntryEnumerator(this);
        }

        /// <internalonly/>
        IEnumerator IEnumerable.GetEnumerator () {
            return new KeyContainerPermissionAccessEntryEnumerator(this);
        }

        /// <internalonly/>
        void ICollection.CopyTo (Array array, int index) {
            if (array == null)
                throw new ArgumentNullException("array");
            if (array.Rank != 1)
                throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
            if (index < 0 || index >= array.Length)
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            if (index + this.Count > array.Length)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();

            for (int i=0; i < this.Count; i++) {
                array.SetValue(this[i], index);
                index++;
            }
        }

        public void CopyTo (KeyContainerPermissionAccessEntry[] array, int index) {
            ((ICollection)this).CopyTo(array, index);
        }

        public bool IsSynchronized {
            get {
                return false;
            }
        }

        public Object SyncRoot {
            get {
                return this;
            }
        }
    }

    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class KeyContainerPermissionAccessEntryEnumerator : IEnumerator {
        private KeyContainerPermissionAccessEntryCollection m_entries;
        private int m_current;

        private KeyContainerPermissionAccessEntryEnumerator () {}
        internal KeyContainerPermissionAccessEntryEnumerator (KeyContainerPermissionAccessEntryCollection entries) {
            m_entries = entries;
            m_current = -1;
        }

        public KeyContainerPermissionAccessEntry Current {
            get {
                return m_entries[m_current];
            }
        }

        /// <internalonly/>
        Object IEnumerator.Current {
            get {
                return (Object) m_entries[m_current];
            }
        }

        public bool MoveNext() {
            if (m_current == ((int) m_entries.Count - 1))
                return false;
            m_current++;
            return true;
        }

        public void Reset() {
            m_current = -1;
        }
    }

    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class KeyContainerPermission : CodeAccessPermission, IUnrestrictedPermission, IBuiltInPermission {
        private KeyContainerPermissionFlags m_flags;
        private KeyContainerPermissionAccessEntryCollection m_accessEntries;

        public KeyContainerPermission (PermissionState state) {
            if (state == PermissionState.Unrestricted)
                m_flags = KeyContainerPermissionFlags.AllFlags;
            else if (state == PermissionState.None)
                m_flags = KeyContainerPermissionFlags.NoFlags;
            else
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPermissionState"));
            m_accessEntries = new KeyContainerPermissionAccessEntryCollection(m_flags);
         }

        public KeyContainerPermission (KeyContainerPermissionFlags flags) {
            VerifyFlags(flags);
            m_flags = flags;
            m_accessEntries = new KeyContainerPermissionAccessEntryCollection(m_flags);
        }

        public KeyContainerPermission (KeyContainerPermissionFlags flags, KeyContainerPermissionAccessEntry[] accessList) {
            if (accessList == null) 
                throw new ArgumentNullException("accessList");
            Contract.EndContractBlock();

            VerifyFlags(flags);
            m_flags = flags;
            m_accessEntries = new KeyContainerPermissionAccessEntryCollection(m_flags);
            for (int index = 0; index < accessList.Length; index++) {
                m_accessEntries.Add(accessList[index]);
            }
        }

        public KeyContainerPermissionFlags Flags {
            get {
                return m_flags;
            }
        }

        public KeyContainerPermissionAccessEntryCollection AccessEntries {
            get {
                return m_accessEntries;
            }
        }

        public bool IsUnrestricted () {
            if (m_flags != KeyContainerPermissionFlags.AllFlags)
                return false;

            foreach (KeyContainerPermissionAccessEntry accessEntry in AccessEntries) {
                if ((accessEntry.Flags & KeyContainerPermissionFlags.AllFlags) != KeyContainerPermissionFlags.AllFlags)
                    return false;
            }

            return true;
        }

        private bool IsEmpty () {
            if (this.Flags == KeyContainerPermissionFlags.NoFlags) {
                foreach (KeyContainerPermissionAccessEntry accessEntry in AccessEntries) {
                    if (accessEntry.Flags != KeyContainerPermissionFlags.NoFlags)
                        return false;
                }
                return true;
            }
            return false;
        }

        //
        // IPermission implementation
        //

        public override bool IsSubsetOf (IPermission target) {
            if (target == null) 
                return IsEmpty();

            if (!VerifyType(target))
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));

            KeyContainerPermission operand = (KeyContainerPermission) target;

            // since there are containers that are neither in the access list of the source, nor in the 
            // access list of the target, the source flags must be a subset of the target flags.
            if ((this.m_flags & operand.m_flags) != this.m_flags)
                return false;

            // Any entry in the source should have "applicable" flags in the destination that actually
            // are less restrictive than the flags in the source.

            foreach (KeyContainerPermissionAccessEntry accessEntry in AccessEntries) {
                KeyContainerPermissionFlags targetFlags = GetApplicableFlags(accessEntry, operand);
                if ((accessEntry.Flags & targetFlags) != accessEntry.Flags)
                    return false;
            }

            // Any entry in the target should have "applicable" flags in the source that actually
            // are more restrictive than the flags in the target.

            foreach (KeyContainerPermissionAccessEntry accessEntry in operand.AccessEntries) {
                KeyContainerPermissionFlags sourceFlags = GetApplicableFlags(accessEntry, this);
                if ((sourceFlags & accessEntry.Flags) != sourceFlags)
                    return false;
            }

            return true;
        }

        public override IPermission Intersect (IPermission target) {
            if (target == null)
                return null;

            if (!VerifyType(target))
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));

            KeyContainerPermission operand = (KeyContainerPermission) target;
            if (this.IsEmpty() || operand.IsEmpty())
                return null;

            KeyContainerPermissionFlags flags_intersect = operand.m_flags & this.m_flags;
            KeyContainerPermission cp = new KeyContainerPermission(flags_intersect);
            foreach (KeyContainerPermissionAccessEntry accessEntry in AccessEntries) {
                cp.AddAccessEntryAndIntersect(accessEntry, operand);
            }
            foreach (KeyContainerPermissionAccessEntry accessEntry in operand.AccessEntries) {
                cp.AddAccessEntryAndIntersect(accessEntry, this);
            }
            return cp.IsEmpty() ? null : cp;
        }

        public override IPermission Union (IPermission target) {
            if (target == null)
                return this.Copy();

            if (!VerifyType(target))
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));            

            KeyContainerPermission operand = (KeyContainerPermission) target;    
            if (this.IsUnrestricted() || operand.IsUnrestricted())
                return new KeyContainerPermission(PermissionState.Unrestricted);

            KeyContainerPermissionFlags flags_union = (KeyContainerPermissionFlags) (m_flags | operand.m_flags);
            KeyContainerPermission cp = new KeyContainerPermission(flags_union);
            foreach (KeyContainerPermissionAccessEntry accessEntry in AccessEntries) {
                cp.AddAccessEntryAndUnion(accessEntry, operand);
            }
            foreach (KeyContainerPermissionAccessEntry accessEntry in operand.AccessEntries) {
                cp.AddAccessEntryAndUnion(accessEntry, this);
            }
            return cp.IsEmpty() ? null : cp;
        }

        public override IPermission Copy () {
            if (this.IsEmpty())
                return null;

            KeyContainerPermission cp = new KeyContainerPermission((KeyContainerPermissionFlags)m_flags);
            foreach (KeyContainerPermissionAccessEntry accessEntry in AccessEntries) {
                cp.AccessEntries.Add(accessEntry);
            }
            return cp;
        }

#if FEATURE_CAS_POLICY
        public override SecurityElement ToXml () {
            SecurityElement securityElement = CodeAccessPermission.CreatePermissionElement(this, "System.Security.Permissions.KeyContainerPermission");
            if (!IsUnrestricted()) {
                securityElement.AddAttribute("Flags", m_flags.ToString());
                if (AccessEntries.Count > 0) {
                    SecurityElement al = new SecurityElement("AccessList");
                    foreach (KeyContainerPermissionAccessEntry accessEntry in AccessEntries) {
                        SecurityElement entryElem = new SecurityElement("AccessEntry");
                        entryElem.AddAttribute("KeyStore", accessEntry.KeyStore);
                        entryElem.AddAttribute("ProviderName", accessEntry.ProviderName);
                        entryElem.AddAttribute("ProviderType", accessEntry.ProviderType.ToString(null, null));
                        entryElem.AddAttribute("KeyContainerName", accessEntry.KeyContainerName);
                        entryElem.AddAttribute("KeySpec", accessEntry.KeySpec.ToString(null, null));
                        entryElem.AddAttribute("Flags", accessEntry.Flags.ToString());
                        al.AddChild(entryElem);
                    }
                    securityElement.AddChild(al);
                }
            } else 
                securityElement.AddAttribute("Unrestricted", "true");

            return securityElement;
        }

        public override void FromXml (SecurityElement securityElement) {
            CodeAccessPermission.ValidateElement(securityElement, this);
            if (XMLUtil.IsUnrestricted(securityElement)) {
                m_flags = KeyContainerPermissionFlags.AllFlags;
                m_accessEntries = new KeyContainerPermissionAccessEntryCollection(m_flags);
                return;
            }

            m_flags = KeyContainerPermissionFlags.NoFlags;
            string strFlags = securityElement.Attribute("Flags");
            if (strFlags != null) {
                KeyContainerPermissionFlags flags = (KeyContainerPermissionFlags) Enum.Parse(typeof(KeyContainerPermissionFlags), strFlags);
                VerifyFlags(flags);
                m_flags = flags;
            }
            m_accessEntries = new KeyContainerPermissionAccessEntryCollection(m_flags);

            if (securityElement.InternalChildren != null && securityElement.InternalChildren.Count != 0) { 
                IEnumerator enumerator = securityElement.Children.GetEnumerator();
                while (enumerator.MoveNext()) {
                    SecurityElement current = (SecurityElement) enumerator.Current;
                    if (current != null) {
                        if (String.Equals(current.Tag, "AccessList"))
                            AddAccessEntries(current);
                    }
                }
            }
        }
#endif // FEATURE_CAS_POLICY

        /// <internalonly/>
        int IBuiltInPermission.GetTokenIndex () {
            return KeyContainerPermission.GetTokenIndex();
        }

        //
        // private methods
        //

        private void AddAccessEntries(SecurityElement securityElement) {
            if (securityElement.InternalChildren != null && securityElement.InternalChildren.Count != 0) {
                IEnumerator elemEnumerator = securityElement.Children.GetEnumerator();
                while (elemEnumerator.MoveNext()) {
                    SecurityElement current = (SecurityElement) elemEnumerator.Current;
                    if (current != null) {
                        if (String.Equals(current.Tag, "AccessEntry")) {
                            int iMax = current.m_lAttributes.Count;
                            Contract.Assert(iMax % 2 == 0, "Odd number of strings means the attr/value pairs were not added correctly");
                            string keyStore = null;
                            string providerName = null;
                            int providerType = -1;
                            string keyContainerName = null;
                            int keySpec = -1;
                            KeyContainerPermissionFlags flags = KeyContainerPermissionFlags.NoFlags;
                            for (int i = 0; i < iMax; i += 2) {
                                String strAttrName = (String) current.m_lAttributes[i];
                                String strAttrValue = (String) current.m_lAttributes[i+1]; 
                                if (String.Equals(strAttrName, "KeyStore"))
                                    keyStore = strAttrValue;
                                if (String.Equals(strAttrName, "ProviderName"))
                                    providerName = strAttrValue;
                                else if (String.Equals(strAttrName, "ProviderType"))
                                    providerType = Convert.ToInt32(strAttrValue, null);
                                else if (String.Equals(strAttrName, "KeyContainerName"))
                                    keyContainerName = strAttrValue;
                                else if (String.Equals(strAttrName, "KeySpec"))
                                    keySpec = Convert.ToInt32(strAttrValue, null);
                                else if (String.Equals(strAttrName, "Flags")) {
                                    flags = (KeyContainerPermissionFlags) Enum.Parse(typeof(KeyContainerPermissionFlags), strAttrValue);
                                }
                            }
                            KeyContainerPermissionAccessEntry accessEntry = new KeyContainerPermissionAccessEntry(keyStore, providerName, providerType, keyContainerName, keySpec, flags);
                            AccessEntries.Add(accessEntry);
                        }
                    }
                }
            }
        }

        private void AddAccessEntryAndUnion (KeyContainerPermissionAccessEntry accessEntry, KeyContainerPermission target) {
            KeyContainerPermissionAccessEntry newAccessEntry = new KeyContainerPermissionAccessEntry(accessEntry);
            newAccessEntry.Flags |= GetApplicableFlags(accessEntry, target);
            AccessEntries.Add(newAccessEntry);
        }

        private void AddAccessEntryAndIntersect (KeyContainerPermissionAccessEntry accessEntry, KeyContainerPermission target) {
            KeyContainerPermissionAccessEntry newAccessEntry = new KeyContainerPermissionAccessEntry(accessEntry);
            newAccessEntry.Flags &= GetApplicableFlags(accessEntry, target);
            AccessEntries.Add(newAccessEntry);
        }

        //
        // private/internal static methods.
        //

        internal static void VerifyFlags (KeyContainerPermissionFlags flags) {
            if ((flags & ~KeyContainerPermissionFlags.AllFlags) != 0)
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", (int)flags));
            Contract.EndContractBlock();
        }

        private static KeyContainerPermissionFlags GetApplicableFlags (KeyContainerPermissionAccessEntry accessEntry, KeyContainerPermission target) {
            KeyContainerPermissionFlags flags = KeyContainerPermissionFlags.NoFlags;
            bool applyDefaultFlags = true;

            // If the entry exists in the target, return the flag of the target entry.
            int index = target.AccessEntries.IndexOf(accessEntry);
            if (index != -1) {
                flags = ((KeyContainerPermissionAccessEntry)target.AccessEntries[index]).Flags;
                return flags;
            }

            // Intersect the flags in all the target entries that apply to the current access entry, 
            foreach (KeyContainerPermissionAccessEntry targetAccessEntry in target.AccessEntries) {
                if (accessEntry.IsSubsetOf(targetAccessEntry)) {
                    if (applyDefaultFlags == false) {
                        flags &= targetAccessEntry.Flags;
                    } else {
                        flags = targetAccessEntry.Flags;
                        applyDefaultFlags = false;
                    }
                }
            }

            // If no target entry applies to the current entry, the default global flag applies.
            if (applyDefaultFlags)
                flags = target.Flags;

            return flags;
        }

        private static int GetTokenIndex() {
            return BuiltInPermissionIndex.KeyContainerPermissionIndex;
        }
    }
}
