// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    /// Provides information about a context to a type converter or a value editor,
    /// so that the type converter or editor can perform a conversion.
    /// </summary>
    public interface ITypeDescriptorContext : IServiceProvider
    {
        /// <summary>
        /// Gets the container with the set of objects for this formatter.
        /// </summary>
        IContainer Container { get; }

        /// <summary>
        /// Gets the instance that is invoking the method on the formatter object.
        /// </summary>
        object Instance { get; }

        /// <summary>
        /// Retrieves the PropertyDescriptor that is surfacing the given context item.
        /// </summary>
        PropertyDescriptor PropertyDescriptor { get; }

        /// <summary>
        /// Gets a value indicating whether this object can be changed.
        /// </summary>
        bool OnComponentChanging();

        /// <summary>
        /// Raises the <see cref='System.ComponentModel.Design.IComponentChangeService.ComponentChanged'/>
        /// event.
        /// </summary>
        void OnComponentChanged();
    }
}
