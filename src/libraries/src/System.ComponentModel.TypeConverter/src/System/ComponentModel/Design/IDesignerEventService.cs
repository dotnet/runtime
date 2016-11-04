// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System;
using System.ComponentModel;
using Microsoft.Win32;

namespace System.ComponentModel.Design
{
    /// <summary>
    ///    <para>Provides global
    ///       event notifications and the ability to create designers.</para>
    /// </summary>
    public interface IDesignerEventService
    {
        /// <summary>
        ///    <para>
        ///       Gets the currently active designer.
        ///    </para>
        /// </summary>
        IDesignerHost ActiveDesigner { get; }

        /// <summary>
        ///    <para>
        ///       Gets or
        ///       sets a collection of running design documents in the development environment.
        ///    </para>
        /// </summary>
        DesignerCollection Designers { get; }

        /// <summary>
        ///    <para>
        ///       Adds an event that will be raised when the currently active designer
        ///       changes.
        ///    </para>
        /// </summary>
        event ActiveDesignerEventHandler ActiveDesignerChanged;

        /// <summary>
        ///    <para>
        ///       Adds an event that will be raised when a designer is created.
        ///    </para>
        /// </summary>
        event DesignerEventHandler DesignerCreated;

        /// <summary>
        ///    <para>
        ///       Adds an event that will be raised when a designer is disposed.
        ///    </para>
        /// </summary>
        event DesignerEventHandler DesignerDisposed;

        /// <summary>
        ///    <para>
        ///       Adds an event that will be raised when the global selection changes.
        ///    </para>
        /// </summary>
        event EventHandler SelectionChanged;
    }
}

