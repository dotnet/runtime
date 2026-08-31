// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Resources;

namespace System.ComponentModel.Design
{
    /// <summary>
    /// Provides designers a way to access a resource for the current design-time object.
    /// </summary>
    public interface IResourceService
    {
        /// <summary>
        /// Locates the resource reader for the specified culture and
        /// returns it.
        /// </summary>
        IResourceReader? GetResourceReader(CultureInfo info);

        /// <summary>
        /// Locates the resource writer for the specified culture
        /// and returns it. This will create a new resource for
        /// the specified culture and destroy any existing resource,
        /// should it exist.
        /// </summary>
        IResourceWriter GetResourceWriter(CultureInfo info);
    }
}
