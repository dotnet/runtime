// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Serialization
{
    using System;
    using System.Reflection;
    using System.Collections;
    using System.ComponentModel;

    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlArrayItemAttributes : CollectionBase
    {
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlArrayItemAttribute this[int index]
        {
            get { return (XmlArrayItemAttribute)List[index]; }
            set { List[index] = value; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int Add(XmlArrayItemAttribute attribute)
        {
            return List.Add(attribute);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Insert(int index, XmlArrayItemAttribute attribute)
        {
            List.Insert(index, attribute);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int IndexOf(XmlArrayItemAttribute attribute)
        {
            return List.IndexOf(attribute);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool Contains(XmlArrayItemAttribute attribute)
        {
            return List.Contains(attribute);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Remove(XmlArrayItemAttribute attribute)
        {
            List.Remove(attribute);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void CopyTo(XmlArrayItemAttribute[] array, int index)
        {
            List.CopyTo(array, index);
        }
    }
}
