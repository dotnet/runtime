// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.ComponentModel
{
    /// <summary>
    ///    <para> 
    ///       Provides information about a context to a type converter or a value editor,
    ///       so that the type converter or editor can perform a conversion.</para>
    /// </summary>
    public interface ITypeDescriptorContext : IServiceProvider
    {
        /// <summary>
        ///    <para>Gets the container with the set of objects for this formatter.</para>
        /// </summary>
        IContainer Container { get; }

        /// <summary>
        ///    <para>Gets the instance that is invoking the method on the formatter object.</para>
        /// </summary>
        object Instance { get; }

        /// <summary>
        ///      Retrieves the PropertyDescriptor that is surfacing the given context item.
        /// </summary>
        PropertyDescriptor PropertyDescriptor { get; }

        /// <summary>
        ///    <para>Gets a value indicating whether this object can be changed.</para>
        /// </summary>
        bool OnComponentChanging();

        /// <summary>
        /// <para>Raises the <see cref='System.ComponentModel.Design.IComponentChangeService.ComponentChanged'/>
        /// event.</para>
        /// </summary>
        void OnComponentChanged();
    }
}

