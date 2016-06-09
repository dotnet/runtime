// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Serialization
{
    using System;
    using System.Reflection;
    using System.Collections;
    using System.ComponentModel;

    /// <include file='doc\XmlElementAttributes.uex' path='docs/doc[@for="XmlElementAttributes"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlElementAttributes : CollectionBase
    {
        /// <include file='doc\XmlElementAttributes.uex' path='docs/doc[@for="XmlElementAttributes.this"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlElementAttribute this[int index]
        {
            get { return (XmlElementAttribute)List[index]; }
            set { List[index] = value; }
        }

        /// <include file='doc\XmlElementAttributes.uex' path='docs/doc[@for="XmlElementAttributes.Add"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int Add(XmlElementAttribute attribute)
        {
            return List.Add(attribute);
        }

        /// <include file='doc\XmlElementAttributes.uex' path='docs/doc[@for="XmlElementAttributes.Insert"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Insert(int index, XmlElementAttribute attribute)
        {
            List.Insert(index, attribute);
        }

        /// <include file='doc\XmlElementAttributes.uex' path='docs/doc[@for="XmlElementAttributes.IndexOf"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int IndexOf(XmlElementAttribute attribute)
        {
            return List.IndexOf(attribute);
        }

        /// <include file='doc\XmlElementAttributes.uex' path='docs/doc[@for="XmlElementAttributes.Contains"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool Contains(XmlElementAttribute attribute)
        {
            return List.Contains(attribute);
        }

        /// <include file='doc\XmlElementAttributes.uex' path='docs/doc[@for="XmlElementAttributes.Remove"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Remove(XmlElementAttribute attribute)
        {
            List.Remove(attribute);
        }

        /// <include file='doc\XmlElementAttributes.uex' path='docs/doc[@for="XmlElementAttributes.CopyTo"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void CopyTo(XmlElementAttribute[] array, int index)
        {
            List.CopyTo(array, index);
        }
    }
}
