// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    /// Containers use sites to manage and communicate their child components.
    /// </summary>
    /// <remarks>
    /// A site is a convenient place for a container to store container-specific
    /// per-component information. The canonical example of such a piece of
    /// information is the name of the component.
    /// Sites bind a <see cref='System.ComponentModel.Component'/> to a
    /// <see cref='System.ComponentModel.IContainer'/> and enable communication between
    /// them, as well as provide a way for the container to manage its components.
    /// </remarks>
    public interface ISite : IServiceProvider
    {
        /// <summary>
        /// When implemented by a class, gets the component associated with
        /// the <see cref='System.ComponentModel.ISite'/>.
        /// </summary>
        IComponent Component { get; }

        /// <summary>
        /// When implemented by a class, gets the container associated with
        /// the <see cref='System.ComponentModel.ISite'/>.
        /// </summary>
        IContainer Container { get; }

        /// <summary>
        /// When implemented by a class, determines whether the component is in design mode.
        /// </summary>
        bool DesignMode { get; }

        /// <summary>
        /// When implemented by a class, gets or sets the name of the component
        /// associated with the <see cref='System.ComponentModel.ISite'/>.
        /// </summary>
        string Name { get; set; }
    }
}
