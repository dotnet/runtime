// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Diagnostics;
using System.Xml.Schema;

namespace System.Xml
{
    internal class XmlName
    {
        string prefix;
        string localName;
        string ns;
        string name;
        int hashCode;
        internal XmlDocument ownerDoc;
        internal XmlName next;

        public static XmlName Create(string prefix, string localName, string ns, int hashCode, XmlDocument ownerDoc, XmlName next)
        {
            return new XmlName(prefix, localName, ns, hashCode, ownerDoc, next);
        }

        internal XmlName(string prefix, string localName, string ns, int hashCode, XmlDocument ownerDoc, XmlName next)
        {
            this.prefix = prefix;
            this.localName = localName;
            this.ns = ns;
            this.name = null;
            this.hashCode = hashCode;
            this.ownerDoc = ownerDoc;
            this.next = next;
        }

        public string LocalName
        {
            get
            {
                return localName;
            }
        }

        public string NamespaceURI
        {
            get
            {
                return ns;
            }
        }

        public string Prefix
        {
            get
            {
                return prefix;
            }
        }

        public int HashCode
        {
            get
            {
                return hashCode;
            }
        }

        public XmlDocument OwnerDocument
        {
            get
            {
                return ownerDoc;
            }
        }

        public string Name
        {
            get
            {
                if (name == null)
                {
                    Debug.Assert(prefix != null);
                    if (prefix.Length > 0)
                    {
                        if (localName.Length > 0)
                        {
                            string n = string.Concat(prefix, ":", localName);
                            lock (ownerDoc.NameTable)
                            {
                                if (name == null)
                                {
                                    name = ownerDoc.NameTable.Add(n);
                                }
                            }
                        }
                        else
                        {
                            name = prefix;
                        }
                    }
                    else
                    {
                        name = localName;
                    }
                    Debug.Assert(Ref.Equal(name, ownerDoc.NameTable.Get(name)));
                }
                return name;
            }
        }

        public virtual bool IsDefault
        {
            get
            {
                return false;
            }
        }

        public virtual bool IsNil
        {
            get
            {
                return false;
            }
        }
    }
}
