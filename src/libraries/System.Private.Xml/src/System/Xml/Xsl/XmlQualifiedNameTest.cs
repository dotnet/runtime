// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml;
using System.Xml.Schema;

namespace System.Xml.Xsl
{
    /// <summary>
    /// XmlQualifiedName extends XmlQualifiedName to support wildcards and adds nametest functionality
    /// Following are the examples:
    ///     {A}:B     XmlQualifiedNameTest.New("B", "A")        Match QName with namespace A        and local name B
    ///     *         XmlQualifiedNameTest.New(null, null)      Match any QName
    ///     {A}:*     XmlQualifiedNameTest.New(null, "A")       Match QName with namespace A        and any local name
    ///               XmlQualifiedNameTest.New("A", false)
    ///     *:B       XmlQualifiedNameTest.New("B", null)       Match QName with any namespace      and local name B
    ///     ~{A}:*    XmlQualifiedNameTest.New("B", "A")        Match QName with namespace not A    and any local name
    ///     {~A}:B    only as a result of the intersection      Match QName with namespace not A    and local name B
    /// </summary>
    internal sealed class XmlQualifiedNameTest : XmlQualifiedName
    {
        private readonly bool _exclude;
        private const string wildcard = "*";
        private static readonly XmlQualifiedNameTest s_wc = XmlQualifiedNameTest.New(wildcard, wildcard);

        /// <summary>
        /// Full wildcard
        /// </summary>
        public static XmlQualifiedNameTest Wildcard
        {
            get { return s_wc; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        private XmlQualifiedNameTest(string? name, string? ns, bool exclude) : base(name, ns)
        {
            _exclude = exclude;
        }

        /// <summary>
        /// Construct new from name and namespace. Returns singleton Wildcard in case full wildcard
        /// </summary>
        public static XmlQualifiedNameTest New(string? name, string? ns)
        {
            if (ns == null && name == null)
            {
                return Wildcard;
            }
            else
            {
                return new XmlQualifiedNameTest(name ?? wildcard, ns ?? wildcard, false);
            }
        }

        /// <summary>
        /// True if matches any name and any namespace
        /// </summary>
        public bool IsWildcard
        {
            get { return (object)this == (object)Wildcard; }
        }

        /// <summary>
        /// True if matches any name
        /// </summary>
        public bool IsNameWildcard
        {
            get { return (object)this.Name == (object)wildcard; }
        }

        /// <summary>
        /// True if matches any namespace
        /// </summary>
        public bool IsNamespaceWildcard
        {
            get { return (object)this.Namespace == (object)wildcard; }
        }

        private bool IsNameSubsetOf(XmlQualifiedNameTest other)
        {
            return other.IsNameWildcard || this.Name == other.Name;
        }

        // BUGBUG - exclude local
        private bool IsNamespaceSubsetOf(XmlQualifiedNameTest other)
        {
            return other.IsNamespaceWildcard
                || (_exclude == other._exclude && this.Namespace == other.Namespace)
                || (other._exclude && !_exclude && this.Namespace != other.Namespace);
        }

        /// <summary>
        /// True if this matches every QName other does
        /// </summary>
        public bool IsSubsetOf(XmlQualifiedNameTest other)
        {
            return IsNameSubsetOf(other) && IsNamespaceSubsetOf(other);
        }

        /// <summary>
        /// Return true if the result of intersection with other is not empty
        /// </summary>
        public bool HasIntersection(XmlQualifiedNameTest other)
        {
            return (IsNamespaceSubsetOf(other) || other.IsNamespaceSubsetOf(this)) && (IsNameSubsetOf(other) || other.IsNameSubsetOf(this));
        }

        /// <summary>
        /// String representation
        /// </summary>
        public override string ToString()
        {
            if ((object)this == (object)Wildcard)
            {
                return "*";
            }
            else
            {
                if (this.Namespace.Length == 0)
                {
                    return this.Name;
                }
                else if ((object)this.Namespace == (object)wildcard)
                {
                    return $"*:{this.Name}";
                }
                else if (_exclude)
                {
                    return $"{{~{this.Namespace}}}:{this.Name}";
                }
                else
                {
                    return $"{{{this.Namespace}}}:{this.Name}";
                }
            }
        }

#if SchemaTypeImport
        /// <summary>
        /// Construct new from XmlQualifiedName. Returns singleton Wildcard in case full wildcard
        /// </summary>
        public static XmlQualifiedNameTest New(XmlQualifiedName name) {
            if (name.IsEmpty) {
                return Wildcard;
            }
            else {
                return new XmlQualifiedNameTest(name.Name, name.Namespace, false);
            }
        }

        /// <summary>
        /// Construct new "exclusion" name test
        /// </summary>
        public static XmlQualifiedNameTest New(string ns, bool exclude) {
            Debug.Assert(ns != null);
            return new XmlQualifiedNameTest(wildcard, ns, exclude);
        }

        /// <summary>
        /// Return the result of intersection with other
        /// </summary>
        public XmlQualifiedNameTest Intersect(XmlQualifiedNameTest other) {
            // Namespace
            // this\other   ~y                          *               y
            //        ~x    x=y ? this|other : null     this            x!=y ? other : null
            //         *    other                       this|other      other
            //         x    x!=y ? this : null          this            x=y ? this|other : null
            XmlQualifiedNameTest namespaceFrom = IsNamespaceSubsetOf(other) ? this : other.IsNamespaceSubsetOf(this) ? other : null;
            XmlQualifiedNameTest nameFrom = IsNameSubsetOf(other) ? this : other.IsNameSubsetOf(this) ? other : null;

            if ((object)namespaceFrom == (object)nameFrom) {
                return namespaceFrom;
            }
            else if (namespaceFrom == null || nameFrom == null) {
                return null;
            }
            else {
                return new XmlQualifiedNameTest(nameFrom.Name, namespaceFrom.Namespace, namespaceFrom.ExcludeNamespace);
            }
        }

        /// <summary>
        /// True if neither name nor namespace is a wildcard
        /// </summary>
        public bool IsSingleName {
            get { return (object)this.Name != (object)wildcard && (object)this.Namespace != (object)wildcard  && this.exclude == false; }
        }

        /// <summary>
        /// True if matches any namespace other then this.Namespace
        /// </summary>
        public bool ExcludeNamespace {
            get { return this.exclude; }
        }
#endif
    }
}
