// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.ComponentModel.Design
{
    /// <summary>
    /// Provides an interface for a designer to select components.
    /// </summary>
    public interface ISelectionService
    {
        /// <summary>
        /// Gets the object that is currently the primary selection.
        /// </summary>
        object PrimarySelection { get; }

        /// <summary>
        /// Gets the count of selected objects.
        /// </summary>
        int SelectionCount { get; }

        /// <summary>
        /// Adds a <see cref='System.ComponentModel.Design.ISelectionService.SelectionChanged'/> event handler to the selection service.
        /// </summary>
        event EventHandler SelectionChanged;

        /// <summary>
        /// Adds an event handler to the selection service.
        /// </summary>
        event EventHandler SelectionChanging;

        /// <summary>
        /// Gets a value indicating whether the component is currently selected.
        /// </summary>
        bool GetComponentSelected(object component);

        /// <summary>
        /// Gets a collection of components that are currently part of the user's selection.
        /// </summary>
        ICollection GetSelectedComponents();

        /// <summary>
        /// Sets the currently selected set of components.
        /// </summary>
        void SetSelectedComponents(ICollection components);

        /// <summary>
        /// Sets the currently selected set of components to those with the specified selection type within the specified array of components.
        /// </summary>
        void SetSelectedComponents(ICollection components, SelectionTypes selectionType);
    }
}
