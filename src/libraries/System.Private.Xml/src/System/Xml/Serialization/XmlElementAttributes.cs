// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.ComponentModel;
using System.Reflection;

namespace System.Xml.Serialization
{
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlElementAttributes : CollectionBase
    {
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlElementAttribute? this[int index]
        {
            get { return (XmlElementAttribute?)List[index]; }
            set { List[index] = value; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int Add(XmlElementAttribute? attribute)
        {
            return List.Add(attribute);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Insert(int index, XmlElementAttribute? attribute)
        {
            List.Insert(index, attribute);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int IndexOf(XmlElementAttribute? attribute)
        {
            return List.IndexOf(attribute);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool Contains(XmlElementAttribute? attribute)
        {
            return List.Contains(attribute);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Remove(XmlElementAttribute? attribute)
        {
            List.Remove(attribute);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void CopyTo(XmlElementAttribute[] array, int index)
        {
            List.CopyTo(array, index);
        }
    }
}
