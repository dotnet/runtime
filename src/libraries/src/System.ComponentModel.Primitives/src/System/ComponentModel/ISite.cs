// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace System.ComponentModel
{
    /*
     * Containers use sites to manage and communicate their child components.
     *
     * A site is a convenient place for a container to store container-specific
     * per-component information.  The canonical example of such a piece of
     * information is the name of the component.
     *
     * To be a site, a class implements the ISite interface.
     */

    /// <devdoc>
    ///    <para>Provides functionality required by sites. Sites bind
    ///       a <see cref='System.ComponentModel.Component'/> to a <see cref='System.ComponentModel.Container'/>
    ///       and enable communication between them, as well as provide a way
    ///       for the container to manage its components.</para>
    /// </devdoc>
    [ComVisible(true)]
    public interface ISite : IServiceProvider
    {
        // The component sited by this component site.
        /// <devdoc>
        ///    <para>When implemented by a class, gets the component associated with the <see cref='System.ComponentModel.ISite'/>.</para>
        /// </devdoc>
        IComponent Component { get; }

        // The container in which the component is sited.
        /// <devdoc>
        /// <para>When implemented by a class, gets the container associated with the <see cref='System.ComponentModel.ISite'/>.</para>
        /// </devdoc>
        IContainer Container { get; }

        // Indicates whether the component is in design mode.
        /// <devdoc>
        ///    <para>When implemented by a class, determines whether the component is in design mode.</para>
        /// </devdoc>
        bool DesignMode { get; }

        // The name of the component.
        //
        /// <devdoc>
        ///    <para>When implemented by a class, gets or sets the name of
        ///       the component associated with the <see cref='System.ComponentModel.ISite'/>.</para>
        /// </devdoc>
        String Name { get; set; }
    }
}
