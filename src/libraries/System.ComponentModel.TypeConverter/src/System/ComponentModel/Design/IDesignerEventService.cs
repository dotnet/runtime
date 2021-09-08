// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.ComponentModel.Design
{
    /// <summary>
    /// Provides global event notifications and the ability to create designers.
    /// </summary>
    public interface IDesignerEventService
    {
        /// <summary>
        /// Gets the currently active designer.
        /// </summary>
        IDesignerHost? ActiveDesigner { get; }

        /// <summary>
        /// Gets or sets a collection of running design documents in the development environment.
        /// </summary>
        DesignerCollection Designers { get; }

        /// <summary>
        /// Adds an event that will be raised when the currently active designer changes.
        /// </summary>
        event ActiveDesignerEventHandler ActiveDesignerChanged;

        /// <summary>
        /// Adds an event that will be raised when a designer is created.
        /// </summary>
        event DesignerEventHandler DesignerCreated;

        /// <summary>
        /// Adds an event that will be raised when a designer is disposed.
        /// </summary>
        event DesignerEventHandler DesignerDisposed;

        /// <summary>
        /// Adds an event that will be raised when the global selection changes.
        /// </summary>
        event EventHandler SelectionChanged;
    }
}
