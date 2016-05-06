// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>[To be supplied.]</para>
    /// </summary>
    public interface ITypedList
    {
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        string GetListName(PropertyDescriptor[] listAccessors);

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        PropertyDescriptorCollection GetItemProperties(PropertyDescriptor[] listAccessors);
    }
}
