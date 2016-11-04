// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Permissions;

namespace System.ComponentModel.Design
{
    /// <summary>
    /// <para>Represents the method that will handle a ComponentChangingEvent event.</para>
    /// </summary>

    public delegate void ComponentChangingEventHandler(object sender, ComponentChangingEventArgs e);
}
