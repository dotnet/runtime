// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace System.ComponentModel
{
    /*
     * A "component" is an object that can be placed in a container.
     *
     * In this context, "containment" refers to logical containment, not visual
     * containment.  Components and containers can be used in a variety of
     * scenarios, including both visual and non-visual scenarios.
     *
     * To be a component, a class implements the IComponent interface, and provides
     * a parameter-less constructor.
     *
     * A component interacts with its container primarily through a container-
     * provided "site".
     */

    /// <devdoc>
    ///    <para>Provides functionality required by all components.</para>
    /// </devdoc>
    [ComVisible(true)]
    public interface IComponent : IDisposable
    {
        // The site of the component.
        /// <devdoc>
        ///    <para>When implemented by a class, gets or sets
        ///       the <see cref='System.ComponentModel.ISite'/> associated
        ///       with the <see cref='System.ComponentModel.IComponent'/>.</para>
        /// </devdoc>
        ISite Site
        {
            get;
            set;
        }

        /// <devdoc>
        ///    <para>Adds a event handler to listen to the Disposed event on the component.</para>
        /// </devdoc>
        event EventHandler Disposed;
    }
}
