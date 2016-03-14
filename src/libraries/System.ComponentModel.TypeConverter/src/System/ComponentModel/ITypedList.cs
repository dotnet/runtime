// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;

namespace System.ComponentModel
{
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public interface ITypedList
    {
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        string GetListName(PropertyDescriptor[] listAccessors);
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        PropertyDescriptorCollection GetItemProperties(PropertyDescriptor[] listAccessors);
    }
}
