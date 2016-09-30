// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System;
using System.ComponentModel;

namespace System.ComponentModel.Design
{
    /// <summary>
    ///    <para>Provides an interface to list extender providers.</para>
    /// </summary>
    public interface IExtenderListService
    {
        /// <summary>
        ///    <para>Gets the set of extender providers for the component.</para>
        /// </summary>
        IExtenderProvider[] GetExtenderProviders();
    }
}
