// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Xml
{
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlQualifiedName
    {
        private int _hash;

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static readonly XmlQualifiedName Empty = new(string.Empty);

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlQualifiedName() : this(string.Empty, string.Empty) { }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlQualifiedName(string? name) : this(name, string.Empty) { }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlQualifiedName(string? name, string? ns)
        {
            Namespace = ns ?? string.Empty;
            Name = name ?? string.Empty;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string Namespace { get; private set; }
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string Name { get; private set; }
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public override int GetHashCode()
        {
            if (_hash == 0)
            {
                _hash = Name.GetHashCode(); /*+ Namespace.GetHashCode()*/ // for perf reasons we are not taking ns's hashcode.
            }
            return _hash;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool IsEmpty => Name.Length == 0 && Namespace.Length == 0;
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public override string ToString()
        {
            return Namespace.Length == 0 ? Name : $"{Namespace}:{Name}";
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public override bool Equals([NotNullWhen(true)] object? other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return other is XmlQualifiedName qName && Equals(qName.Name, qName.Namespace);
        }

        internal bool Equals(string name, string ns) => Name == name && Namespace == ns;

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static bool operator ==(XmlQualifiedName? a, XmlQualifiedName? b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (a is null || b is null)
            {
                return false;
            }

            return a.Name == b.Name && a.Namespace == b.Namespace;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static bool operator !=(XmlQualifiedName? a, XmlQualifiedName? b)
        {
            return !(a == b);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static string ToString(string name, string? ns)
        {
            return ns == null || ns.Length == 0 ? name : $"{ns}:{name}";
        }

        // --------- Some useful internal stuff -----------------
        internal void Init(string? name, string? ns)
        {
            Name = name ?? string.Empty;
            Namespace = ns ?? string.Empty;
            _hash = 0;
        }

        internal void SetNamespace(string? ns)
        {
            Namespace = ns ?? string.Empty; // Not changing hash since ns is not used to compute hashcode
        }

        internal void Verify()
        {
            XmlConvert.VerifyNCName(Name);
            if (Namespace.Length != 0)
            {
                XmlConvert.ToUri(Namespace);
            }
        }

        internal void Atomize(XmlNameTable nameTable)
        {
            Name = nameTable.Add(Name);
            Namespace = nameTable.Add(Namespace);
        }

        internal static XmlQualifiedName Parse(string s, IXmlNamespaceResolver nsmgr, out string prefix)
        {
            ValidateNames.ParseQNameThrow(s, out prefix, out string localName);

            string? uri = nsmgr.LookupNamespace(prefix);
            if (uri == null)
            {
                if (prefix.Length != 0)
                {
                    throw new XmlException(SR.Xml_UnknownNs, prefix);
                }

                // Re-map namespace of empty prefix to string.Empty when there is no default namespace declared
                uri = string.Empty;
            }

            return new XmlQualifiedName(localName, uri);
        }

        internal XmlQualifiedName Clone()
        {
            return (XmlQualifiedName)MemberwiseClone();
        }
    }
}
