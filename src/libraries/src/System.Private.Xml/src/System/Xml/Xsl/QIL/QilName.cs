// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace System.Xml.Xsl.Qil
{
    /// <summary>
    /// View over a Qil name literal.
    /// </summary>
    /// <remarks>
    /// Don't construct QIL nodes directly; instead, use the <see cref="QilFactory">QilFactory</see>.
    /// </remarks>
    internal class QilName : QilLiteral
    {
        private string _local;
        private string _uri;
        private string _prefix;


        //-----------------------------------------------
        // Constructor
        //-----------------------------------------------

        /// <summary>
        /// Construct a new node
        /// </summary>
        public QilName(QilNodeType nodeType, string local, string uri, string prefix) : base(nodeType, null)
        {
            LocalName = local;
            NamespaceUri = uri;
            Prefix = prefix;
            Value = this;
        }


        //-----------------------------------------------
        // QilName methods
        //-----------------------------------------------

        public string LocalName
        {
            get { return _local; }
            set { _local = value; }
        }

        public string NamespaceUri
        {
            get { return _uri; }
            set { _uri = value; }
        }

        public string Prefix
        {
            get { return _prefix; }
            set { _prefix = value; }
        }

        /// <summary>
        /// Build the qualified name in the form prefix:local
        /// </summary>
        public string QualifiedName
        {
            get
            {
                if (_prefix.Length == 0)
                {
                    return _local;
                }
                else
                {
                    return _prefix + ':' + _local;
                }
            }
        }

        /// <summary>
        /// Override GetHashCode() so that the QilName can be used as a key in the hashtable.
        /// </summary>
        /// <remarks>Does not compare their prefixes (if any).</remarks>
        public override int GetHashCode()
        {
            return _local.GetHashCode();
        }

        /// <summary>
        /// Override Equals() so that the QilName can be used as a key in the hashtable.
        /// </summary>
        /// <remarks>Does not compare their prefixes (if any).</remarks>
        public override bool Equals(object other)
        {
            QilName name = other as QilName;
            if (name == null)
                return false;

            return _local == name._local && _uri == name._uri;
        }

        /// <summary>
        /// Implement operator == to prevent accidental referential comparison
        /// </summary>
        /// <remarks>Does not compare their prefixes (if any).</remarks>
        public static bool operator ==(QilName a, QilName b)
        {
            if ((object)a == (object)b)
            {
                return true;
            }
            if ((object)a == null || (object)b == null)
            {
                return false;
            }
            return a._local == b._local && a._uri == b._uri;
        }

        /// <summary>
        /// Implement operator != to prevent accidental referential comparison 
        /// </summary>
        /// <remarks>Does not compare their prefixes (if any).</remarks>
        public static bool operator !=(QilName a, QilName b)
        {
            return !(a == b);
        }

        /// <summary>
        /// Return the QilName in this format: "{namespace}prefix:local-name".
        /// If the namespace is empty, return the QilName in this truncated format: "local-name".
        /// If the prefix is empty, return the QilName in this truncated format: "{namespace}local-name".
        /// </summary>
        public override string ToString()
        {
            if (_prefix.Length == 0)
            {
                if (_uri.Length == 0)
                    return _local;

                return string.Concat("{", _uri, "}", _local);
            }

            return string.Concat("{", _uri, "}", _prefix, ":", _local);
        }
    }
}
