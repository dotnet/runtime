// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    /// Represents the method that will handle the <see cref='System.ComponentModel.TypeDescriptor.Refresh(object)'/> event
    /// raised when a <see cref='System.Type'/> or component is changed during design time.
    /// </summary>
    public delegate void RefreshEventHandler(RefreshEventArgs e);
}
