// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Composition.Hosting
{
    /// <summary>
    ///     Notifications when a ComposablePartCatalog changes.
    /// </summary>
    public interface INotifyComposablePartCatalogChanged
    {
        event EventHandler<ComposablePartCatalogChangeEventArgs> Changed;
        event EventHandler<ComposablePartCatalogChangeEventArgs> Changing;
    }
}
