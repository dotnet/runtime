// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Collections;
using System.ComponentModel;

namespace System.Xml.Serialization
{
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlAnyElementAttributes : CollectionBase
    {
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlAnyElementAttribute? this[int index]
        {
            get { return (XmlAnyElementAttribute?)List[index]; }
            set { List[index] = value; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int Add(XmlAnyElementAttribute? attribute)
        {
            return List.Add(attribute);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Insert(int index, XmlAnyElementAttribute? attribute)
        {
            List.Insert(index, attribute);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int IndexOf(XmlAnyElementAttribute? attribute)
        {
            return List.IndexOf(attribute);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool Contains(XmlAnyElementAttribute? attribute)
        {
            return List.Contains(attribute);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Remove(XmlAnyElementAttribute? attribute)
        {
            List.Remove(attribute);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void CopyTo(XmlAnyElementAttribute[] array, int index)
        {
            List.CopyTo(array, index);
        }
    }
}
