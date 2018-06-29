// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Warning: Do not port implementation of GetHashCode to Desktop. See notes in SecureStringHasher

using System.Collections;
using System.Diagnostics;

namespace System.Xml
{
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlQualifiedName
    {
        private string _name;
        private string _ns;
        private int _hash;

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static readonly XmlQualifiedName Empty = new XmlQualifiedName(string.Empty);

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlQualifiedName() : this(string.Empty, string.Empty) { }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlQualifiedName(string name) : this(name, string.Empty) { }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlQualifiedName(string name, string ns)
        {
            _ns = ns == null ? string.Empty : ns;
            _name = name == null ? string.Empty : name;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string Namespace
        {
            get { return _ns; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string Name
        {
            get { return _name; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public override int GetHashCode()
        {
            if (_hash == 0)
            {
                _hash = Name.GetHashCode() /*+ Namespace.GetHashCode()*/; // for perf reasons we are not taking ns's hashcode.
            }
            return _hash;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool IsEmpty
        {
            get { return Name.Length == 0 && Namespace.Length == 0; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public override string ToString()
        {
            return Namespace.Length == 0 ? Name : string.Concat(Namespace, ":", Name);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public override bool Equals(object other)
        {
            XmlQualifiedName qname;

            if ((object)this == other)
            {
                return true;
            }

            qname = other as XmlQualifiedName;
            if (qname != null)
            {
                return (Name == qname.Name && Namespace == qname.Namespace);
            }
            return false;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static bool operator ==(XmlQualifiedName a, XmlQualifiedName b)
        {
            if ((object)a == (object)b)
                return true;

            if ((object)a == null || (object)b == null)
                return false;

            return (a.Name == b.Name && a.Namespace == b.Namespace);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static bool operator !=(XmlQualifiedName a, XmlQualifiedName b)
        {
            return !(a == b);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static string ToString(string name, string ns)
        {
            return ns == null || ns.Length == 0 ? name : ns + ":" + name;
        }

        // --------- Some useful internal stuff -----------------
        internal void Init(string name, string ns)
        {
            Debug.Assert(name != null && ns != null);
            _name = name;
            _ns = ns;
            _hash = 0;
        }

        internal void SetNamespace(string ns)
        {
            Debug.Assert(ns != null);
            _ns = ns; //Not changing hash since ns is not used to compute hashcode
        }

        internal void Verify()
        {
            XmlConvert.VerifyNCName(_name);
            if (_ns.Length != 0)
            {
                XmlConvert.ToUri(_ns);
            }
        }

        internal void Atomize(XmlNameTable nameTable)
        {
            Debug.Assert(_name != null);
            _name = nameTable.Add(_name);
            _ns = nameTable.Add(_ns);
        }

        internal static XmlQualifiedName Parse(string s, IXmlNamespaceResolver nsmgr, out string prefix)
        {
            string localName;
            ValidateNames.ParseQNameThrow(s, out prefix, out localName);

            string uri = nsmgr.LookupNamespace(prefix);
            if (uri == null)
            {
                if (prefix.Length != 0)
                {
                    throw new XmlException(SR.Xml_UnknownNs, prefix);
                }
                else
                { //Re-map namespace of empty prefix to string.Empty when there is no default namespace declared
                    uri = string.Empty;
                }
            }
            return new XmlQualifiedName(localName, uri);
        }
        internal XmlQualifiedName Clone()
        {
            return (XmlQualifiedName)MemberwiseClone();
        }
    }
}

