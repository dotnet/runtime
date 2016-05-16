// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.ComponentModel
{
    /*
     * A "container" is an object that logically contains zero or more child
     * components.
     *
     * In this context, "containment" refers to logical containment, not visual
     * containment.  Components and containers can be used in a variety of
     * scenarios, including both visual and non-visual scenarios.
     */

    /// <summary>
    ///    <para>Provides
    ///       functionality for containers. Containers are objects that logically contain zero or more components.</para>
    /// </summary>
    public interface IContainer : IDisposable
    {
        // Adds a component to the container.
        /// <summary>
        /// <para>Adds the specified <see cref='System.ComponentModel.IComponent'/> to the <see cref='System.ComponentModel.IContainer'/>
        /// at the end of the list.</para>
        /// </summary>
        void Add(IComponent component);

        //  Adds a component to the container.
        /// <summary>
        /// <para>Adds the specified <see cref='System.ComponentModel.IComponent'/> to the <see cref='System.ComponentModel.IContainer'/>
        /// at the end of the list, and assigns a name to the component.</para>
        /// </summary>
        void Add(IComponent component, String name);

        // The components in the container.
        /// <summary>
        /// <para>Gets all the components in the <see cref='System.ComponentModel.IContainer'/>.</para>
        /// </summary>
        ComponentCollection Components { get; }

        // Removes a component from the container.
        /// <summary>
        /// <para>Removes a component from the <see cref='System.ComponentModel.IContainer'/>.</para>
        /// </summary>
        void Remove(IComponent component);
    }
}
