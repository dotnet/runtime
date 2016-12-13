// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices
{
    using System;
    using System.Runtime.InteropServices;
    using System.Collections;
    using System.Diagnostics;

    /// <include file='doc\ResultPropertyValueCollection.uex' path='docs/doc[@for="ResultPropertyValueCollection"]/*' />
    /// <devdoc>
    ///    <para>Specifies a collection of values for a multi-valued property.</para>
    /// </devdoc>
    public class ResultPropertyValueCollection : ReadOnlyCollectionBase
    {
        internal ResultPropertyValueCollection(object[] values)
        {
            if (values == null)
                values = new object[0];

            InnerList.AddRange(values);
        }

        /// <include file='doc\ResultPropertyValueCollection.uex' path='docs/doc[@for="ResultPropertyValueCollection.this"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public object this[int index]
        {
            get
            {
                object returnValue = InnerList[index];
                if (returnValue is Exception)
                    throw (Exception)returnValue;
                else
                    return returnValue;
            }
        }

        /// <include file='doc\ResultPropertyValueCollection.uex' path='docs/doc[@for="ResultPropertyValueCollection.Contains"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool Contains(object value)
        {
            return InnerList.Contains(value);
        }

        /// <include file='doc\ResultPropertyValueCollection.uex' path='docs/doc[@for="ResultPropertyValueCollection.IndexOf"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int IndexOf(object value)
        {
            return InnerList.IndexOf(value);
        }

        /// <include file='doc\ResultPropertyValueCollection.uex' path='docs/doc[@for="ResultPropertyValueCollection.CopyTo"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void CopyTo(object[] values, int index)
        {
            InnerList.CopyTo(values, index);
        }
    }
}
