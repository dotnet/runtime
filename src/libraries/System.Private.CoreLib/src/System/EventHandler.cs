// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    public delegate void EventHandler(object? sender, EventArgs e);

    public delegate void EventHandler<in TEventArgs>(object? sender, TEventArgs e) // Removed TEventArgs constraint post-.NET 4
        where TEventArgs : allows ref struct;

    public delegate void EventHandler<in TSender, in TEventArgs>(TSender sender, TEventArgs e)
        where TSender : allows ref struct
        where TEventArgs : allows ref struct;
}
