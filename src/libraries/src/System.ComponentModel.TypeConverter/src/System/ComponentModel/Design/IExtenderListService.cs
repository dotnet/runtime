// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Design
{
    /// <summary>
    /// Provides an interface to list extender providers.
    /// </summary>
    public interface IExtenderListService
    {
        /// <summary>
        /// Gets the set of extender providers for the component.
        /// </summary>
        IExtenderProvider[] GetExtenderProviders();
    }
}
