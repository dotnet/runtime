// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Resources;

namespace System.ComponentModel.Design
{
    /// <summary>
    ///    <para> 
    ///       Provides designers a way to
    ///       access a resource for the current design-time
    ///       object.</para>
    /// </summary>
    public interface IResourceService
    {
        /// <summary>
        ///    <para> 
        ///       Locates the resource reader for the specified culture and
        ///       returns it.</para>
        /// </summary>
        IResourceReader GetResourceReader(CultureInfo info);

        /// <summary>
        ///    <para>Locates the resource writer for the specified culture
        ///       and returns it. This will create a new resource for
        ///       the specified culture and destroy any existing resource,
        ///       should it exist.</para>
        /// </summary>
        IResourceWriter GetResourceWriter(CultureInfo info);
    }
}

