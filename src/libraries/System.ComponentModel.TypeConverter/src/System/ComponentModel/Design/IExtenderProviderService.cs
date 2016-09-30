// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System;
using System.ComponentModel;
using Microsoft.Win32;

namespace System.ComponentModel.Design
{
    /// <summary>
    ///    <para>Provides an interface to add and remove extender providers.</para>
    /// </summary>
    public interface IExtenderProviderService
    {
        /// <summary>
        ///    <para>
        ///       Adds an extender provider.
        ///    </para>
        /// </summary>
        void AddExtenderProvider(IExtenderProvider provider);

        /// <summary>
        ///    <para>
        ///       Removes
        ///       an extender provider.
        ///    </para>
        /// </summary>
        void RemoveExtenderProvider(IExtenderProvider provider);
    }
}

