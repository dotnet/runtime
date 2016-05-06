// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>Specifies how the collection is changed.</para>
    /// </summary>
    public enum CollectionChangeAction
    {
        /// <summary>
        ///    <para> Specifies that an element is added to the collection.</para>
        /// </summary>
        Add = 1,

        /// <summary>
        ///    <para>Specifies that an element is removed from the collection.</para>
        /// </summary>
        Remove = 2,

        /// <summary>
        ///    <para>Specifies that the entire collection has changed.</para>
        /// </summary>
        Refresh = 3
    }
}
