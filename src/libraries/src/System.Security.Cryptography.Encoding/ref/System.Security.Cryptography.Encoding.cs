// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Security.Cryptography
{
    public partial class AsnEncodedData
    {
        protected AsnEncodedData() { }
        public AsnEncodedData(byte[] rawData) { }
        public AsnEncodedData(System.Security.Cryptography.AsnEncodedData asnEncodedData) { }
        public AsnEncodedData(System.Security.Cryptography.Oid oid, byte[] rawData) { }
        public AsnEncodedData(string oid, byte[] rawData) { }
        public System.Security.Cryptography.Oid Oid { get { return default(System.Security.Cryptography.Oid); } set { } }
        public byte[] RawData { get { return default(byte[]); } set { } }
        public virtual void CopyFrom(System.Security.Cryptography.AsnEncodedData asnEncodedData) { }
        public virtual string Format(bool multiLine) { return default(string); }
    }
    public sealed partial class Oid
    {
        public Oid(System.Security.Cryptography.Oid oid) { }
        public Oid(string oid) { }
        public Oid(string value, string friendlyName) { }
        public string FriendlyName { get { return default(string); } set { } }
        public string Value { get { return default(string); } set { } }
        public static System.Security.Cryptography.Oid FromFriendlyName(string friendlyName, System.Security.Cryptography.OidGroup group) { return default(System.Security.Cryptography.Oid); }
        public static System.Security.Cryptography.Oid FromOidValue(string oidValue, System.Security.Cryptography.OidGroup group) { return default(System.Security.Cryptography.Oid); }
    }
    public sealed partial class OidCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        public OidCollection() { }
        public int Count { get { return default(int); } }
        public System.Security.Cryptography.Oid this[int index] { get { return default(System.Security.Cryptography.Oid); } }
        public System.Security.Cryptography.Oid this[string oid] { get { return default(System.Security.Cryptography.Oid); } }
        bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
        object System.Collections.ICollection.SyncRoot { get { return default(object); } }
        public int Add(System.Security.Cryptography.Oid oid) { return default(int); }
        public void CopyTo(System.Security.Cryptography.Oid[] array, int index) { }
        public System.Security.Cryptography.OidEnumerator GetEnumerator() { return default(System.Security.Cryptography.OidEnumerator); }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
    }
    public sealed partial class OidEnumerator : System.Collections.IEnumerator
    {
        internal OidEnumerator() { }
        public System.Security.Cryptography.Oid Current { get { return default(System.Security.Cryptography.Oid); } }
        object System.Collections.IEnumerator.Current { get { return default(object); } }
        public bool MoveNext() { return default(bool); }
        public void Reset() { }
    }
    public enum OidGroup
    {
        All = 0,
        Attribute = 5,
        EncryptionAlgorithm = 2,
        EnhancedKeyUsage = 7,
        ExtensionOrAttribute = 6,
        HashAlgorithm = 1,
        KeyDerivationFunction = 10,
        Policy = 8,
        PublicKeyAlgorithm = 3,
        SignatureAlgorithm = 4,
        Template = 9,
    }
}
