// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System;
using System.Collections;
using System.ComponentModel;
using Microsoft.Win32;

namespace System.ComponentModel.Design
{
    /// <summary>
    ///    <para>
    ///       Provides an interface for a designer to select components.
    ///    </para>
    /// </summary>

    public interface ISelectionService
    {
        /// <summary>
        ///    <para>
        ///       Gets the object that is currently the primary selection.
        ///    </para>
        /// </summary>
        object PrimarySelection { get; }

        /// <summary>
        ///    <para>
        ///       Gets the count of selected objects.
        ///    </para>
        /// </summary>
        int SelectionCount { get; }

        /// <summary>
        ///    <para>
        ///       Adds a <see cref='System.ComponentModel.Design.ISelectionService.SelectionChanged'/> event handler to the selection service.
        ///    </para>
        /// </summary>
        event EventHandler SelectionChanged;

        /// <summary>
        ///    <para>
        ///       Adds an event handler to the selection service.
        ///    </para>
        /// </summary>
        event EventHandler SelectionChanging;

        /// <summary>
        ///    <para>Gets a value indicating whether the component is currently selected.</para>
        /// </summary>

        bool GetComponentSelected(object component);

        /// <summary>
        ///    <para>
        ///       Gets a collection of components that are currently part of the user's selection.
        ///    </para>
        /// </summary>
        ICollection GetSelectedComponents();

        /// <summary>
        ///    <para>
        ///       Sets the currently selected set of components.
        ///    </para>
        /// </summary>
        void SetSelectedComponents(ICollection components);

        /// <summary>
        ///    <para>
        ///       Sets the currently selected set of components to those with the specified selection type within the specified array of components.
        ///    </para>
        /// </summary>
        void SetSelectedComponents(ICollection components, SelectionTypes selectionType);
    }
}

