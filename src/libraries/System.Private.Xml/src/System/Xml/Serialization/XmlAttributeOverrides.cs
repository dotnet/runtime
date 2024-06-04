// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Xml.Schema;

namespace System.Xml.Serialization
{
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlAttributeOverrides
    {
        private readonly Dictionary<Type, Dictionary<string, XmlAttributes?>> _types = new Dictionary<Type, Dictionary<string, XmlAttributes?>>();

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Add(Type type, XmlAttributes attributes)
        {
            Add(type, string.Empty, attributes);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Add(Type type, string member, XmlAttributes? attributes)
        {
            Dictionary<string, XmlAttributes?>? members;
            if (!_types.TryGetValue(type, out members))
            {
                members = new Dictionary<string, XmlAttributes?>();
                _types.Add(type, members);
            }
            else if (members.ContainsKey(member))
            {
                throw new InvalidOperationException(SR.Format(SR.XmlAttributeSetAgain, type.FullName, member));
            }
            members.Add(member, attributes);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlAttributes? this[Type type]
        {
            get
            {
                return this[type, string.Empty];
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlAttributes? this[Type type, string member]
        {
            get
            {
                Dictionary<string, XmlAttributes?>? members;
                XmlAttributes? attributes;
                return _types.TryGetValue(type, out members) && members.TryGetValue(member, out attributes)
                    ? attributes
                    : null;
            }
        }
    }
}
