// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Serialization
{
    using System;
    using System.Reflection;
    using System.Collections;
    using System.ComponentModel;

    /// <include file='doc\XmlArrayItemAttributes.uex' path='docs/doc[@for="XmlArrayItemAttributes"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlArrayItemAttributes : CollectionBase
    {
        /// <include file='doc\XmlArrayItemAttributes.uex' path='docs/doc[@for="XmlArrayItemAttributes.this"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlArrayItemAttribute this[int index]
        {
            get { return (XmlArrayItemAttribute)List[index]; }
            set { List[index] = value; }
        }

        /// <include file='doc\XmlArrayItemAttributes.uex' path='docs/doc[@for="XmlArrayItemAttributes.Add"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int Add(XmlArrayItemAttribute attribute)
        {
            return List.Add(attribute);
        }

        /// <include file='doc\XmlArrayItemAttributes.uex' path='docs/doc[@for="XmlArrayItemAttributes.Insert"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Insert(int index, XmlArrayItemAttribute attribute)
        {
            List.Insert(index, attribute);
        }

        /// <include file='doc\XmlArrayItemAttributes.uex' path='docs/doc[@for="XmlArrayItemAttributes.IndexOf"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int IndexOf(XmlArrayItemAttribute attribute)
        {
            return List.IndexOf(attribute);
        }

        /// <include file='doc\XmlArrayItemAttributes.uex' path='docs/doc[@for="XmlArrayItemAttributes.Contains"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool Contains(XmlArrayItemAttribute attribute)
        {
            return List.Contains(attribute);
        }

        /// <include file='doc\XmlArrayItemAttributes.uex' path='docs/doc[@for="XmlArrayItemAttributes.Remove"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Remove(XmlArrayItemAttribute attribute)
        {
            List.Remove(attribute);
        }

        /// <include file='doc\XmlArrayItemAttributes.uex' path='docs/doc[@for="XmlArrayItemAttributes.CopyTo"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void CopyTo(XmlArrayItemAttribute[] array, int index)
        {
            List.CopyTo(array, index);
        }
    }
}
