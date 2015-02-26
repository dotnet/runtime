// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Xml.Schema
{
    internal abstract class SchemaDeclBase
    {
        internal enum Use
        {
            Default,
            Required,
            Implied,
            Fixed,
            RequiredFixed
        };

        protected XmlQualifiedName name = XmlQualifiedName.Empty;
        protected string prefix;
        protected bool isDeclaredInExternal = false;
        protected Use presence;     // the presence, such as fixed, implied, etc


        protected SchemaDeclBase(XmlQualifiedName name, string prefix)
        {
            this.name = name;
            this.prefix = prefix;
        }


        internal XmlQualifiedName Name
        {
            get { return name; }
            set { name = value; }
        }

        internal string Prefix
        {
            get { return (prefix == null) ? string.Empty : prefix; }
            set { prefix = value; }
        }

        internal bool IsDeclaredInExternal
        {
            get { return isDeclaredInExternal; }
            set { isDeclaredInExternal = value; }
        }

        internal Use Presence
        {
            get { return presence; }
            set { presence = value; }
        }
    };
}
