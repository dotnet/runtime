// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Design
{
    /// <summary>
    /// Provides an interface to add and remove extender providers.
    /// </summary>
    public interface IExtenderProviderService
    {
        /// <summary>
        /// Adds an extender provider.
        /// </summary>
        void AddExtenderProvider(IExtenderProvider provider);

        /// <summary>
        /// Removes an extender provider.
        /// </summary>
        void RemoveExtenderProvider(IExtenderProvider provider);
    }
}

