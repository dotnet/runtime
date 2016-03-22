// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Design
{
    /// <devdoc>
    ///    <para>Provides a generic dictionary service that a designer can use
    ///       to store user-defined data on the site.</para>
    /// </devdoc>
    public interface IDictionaryService
    {
        /// <devdoc>
        ///    <para>
        ///       Gets the key corresponding to the specified value.
        ///    </para>
        /// </devdoc>
        object GetKey(object value);

        /// <devdoc>
        ///    <para>
        ///       Gets the value corresponding to the specified key.
        ///    </para>
        /// </devdoc>
        object GetValue(object key);

        /// <devdoc>
        ///    <para> 
        ///       Sets the specified key-value pair.</para>
        /// </devdoc>
        void SetValue(object key, object value);
    }
}
