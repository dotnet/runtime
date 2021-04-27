// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel
{
    /// <summary>
    /// A "container" is an object that logically contains zero or more child
    /// components.
    /// In this context, "containment" refers to logical containment, not visual
    /// containment. Components and containers can be used in a variety of
    /// scenarios, including both visual and non-visual scenarios.
    /// Provides functionality for containers.
    /// </summary>
    public interface IContainer : IDisposable
    {
        /// <summary>
        /// Adds the specified <see cref='System.ComponentModel.IComponent'/> to the
        /// <see cref='System.ComponentModel.IContainer'/> at the end of the list.
        /// </summary>
        void Add(IComponent? component);

        //  Adds a component to the container.
        /// <summary>
        /// Adds the specified <see cref='System.ComponentModel.IComponent'/> to the
        /// <see cref='System.ComponentModel.IContainer'/> at the end of the list,
        /// and assigns a name to the component.
        /// </summary>
        [RequiresUnreferencedCode("The Type of components in the container cannot be statically discovered to validate the name.")]
        void Add(IComponent? component, string? name);

        /// <summary>
        /// Gets all the components in the <see cref='System.ComponentModel.IContainer'/>.
        /// </summary>
        ComponentCollection Components { get; }

        /// <summary>
        /// Removes a component from the <see cref='System.ComponentModel.IContainer'/>.
        /// </summary>
        void Remove(IComponent? component);
    }
}
