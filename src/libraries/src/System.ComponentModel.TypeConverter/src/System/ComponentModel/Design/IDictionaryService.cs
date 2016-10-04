// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;

using System.Diagnostics;

using System;

namespace System.ComponentModel.Design
{
    /// <summary>
    ///    <para>Provides a generic dictionary service that a designer can use
    ///       to store user-defined data on the site.</para>
    /// </summary>
    public interface IDictionaryService
    {
        /// <summary>
        ///    <para>
        ///       Gets the key corresponding to the specified value.
        ///    </para>
        /// </summary>
        object GetKey(object value);

        /// <summary>
        ///    <para>
        ///       Gets the value corresponding to the specified key.
        ///    </para>
        /// </summary>
        object GetValue(object key);

        /// <summary>
        ///    <para> 
        ///       Sets the specified key-value pair.</para>
        /// </summary>
        void SetValue(object key, object value);
    }
}
