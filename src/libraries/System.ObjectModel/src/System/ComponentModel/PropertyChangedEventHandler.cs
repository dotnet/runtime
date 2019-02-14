// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    /// Represents the method that will handle the <see langword='PropertyChanged'/>
    /// event raised when a property is changed on a component.
    /// </summary>
    public delegate void PropertyChangedEventHandler(object sender, PropertyChangedEventArgs e);
}
