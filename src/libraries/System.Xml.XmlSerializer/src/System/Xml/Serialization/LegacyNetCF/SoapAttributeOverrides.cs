// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------------------------
// </copyright>
//------------------------------------------------------------------------------

namespace System.Xml.Serialization.LegacyNetCF
{
    using System.Reflection;
    using System.Collections;
    using System.IO;
    using System.Xml.Schema;
    using System;
    using System.ComponentModel;

    using Hashtable = System.Collections.InternalHashtable;

    [System.Security.FrameworkVisibilitySilverlightInternal]
    public class SoapAttributeOverrides
    {
        private Hashtable _types = new Hashtable();

        public SoapAttributeOverrides() { }

        [System.Security.FrameworkVisibilityCompactFrameworkInternal]
        public void Add(Type type, SoapAttributes attributes)
        {
            Add(type, string.Empty, attributes);
        }

        [System.Security.FrameworkVisibilityCompactFrameworkInternal]
        public void Add(Type type, string member, SoapAttributes attributes)
        {
            Hashtable members = (Hashtable)_types[type];
            if (members == null)
            {
                members = new Hashtable();
                _types.Add(type, members);
            }
            else if (members[member] != null)
            {
                throw new InvalidOperationException(SR.Format(SR.XmlMultipleAttributeOverrides, type.FullName, member));
            }
            members.Add(member, attributes);
        }

        public SoapAttributes this[Type type]
        {
            get
            {
                return this[type, string.Empty];
            }
        }

        public SoapAttributes this[Type type, string member]
        {
            get
            {
                Hashtable members = (Hashtable)_types[type];
                if (members == null) return null;
                return (SoapAttributes)members[member];
            }
        }
    }
}

