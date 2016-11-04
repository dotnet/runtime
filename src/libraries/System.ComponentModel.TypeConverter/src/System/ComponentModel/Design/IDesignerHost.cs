// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System;
using System.ComponentModel;
using Microsoft.Win32;
using System.Reflection;

namespace System.ComponentModel.Design
{
    /// <summary>
    ///    <para>
    ///       Provides methods to adjust the configuration of and retrieve
    ///       information about the services and behavior of a designer.
    ///    </para>
    /// </summary>

    public interface IDesignerHost : IServiceContainer
    {
        /// <summary>
        ///    <para>
        ///       Gets or sets a value indicating whether the designer host
        ///       is currently loading the document.
        ///    </para>
        /// </summary>
        bool Loading { get; }

        /// <summary>
        ///    <para>Gets a value indicating whether the designer host is currently in a transaction.</para>
        /// </summary>
        bool InTransaction { get; }

        /// <summary>
        ///    <para>
        ///       Gets the container for this designer host.
        ///    </para>
        /// </summary>
        IContainer Container { get; }

        /// <summary>
        ///    <para>
        ///       Gets the instance of the base class used as the base class for the current design.
        ///    </para>
        /// </summary>
        IComponent RootComponent { get; }

        /// <summary>
        ///    <para>
        ///       Gets the fully qualified name of the class that is being designed.
        ///    </para>
        /// </summary>
        string RootComponentClassName { get; }

        /// <summary>
        ///    <para>
        ///       Gets the description of the current transaction.
        ///    </para>
        /// </summary>
        string TransactionDescription { get; }

        /// <summary>
        ///    <para>
        ///       Adds an event handler for the <see cref='System.ComponentModel.Design.IDesignerHost.Activated'/> event.
        ///    </para>
        /// </summary>
        event EventHandler Activated;

        /// <summary>
        ///    <para>
        ///       Adds an event handler for the <see cref='System.ComponentModel.Design.IDesignerHost.Deactivated'/> event.
        ///    </para>
        /// </summary>
        event EventHandler Deactivated;

        /// <summary>
        ///    <para>
        ///       Adds an event handler for the <see cref='System.ComponentModel.Design.IDesignerHost.LoadComplete'/> event.
        ///    </para>
        /// </summary>
        event EventHandler LoadComplete;

        /// <summary>
        ///    <para>
        ///       Adds an event handler for the <see cref='System.ComponentModel.Design.IDesignerHost.TransactionClosed'/> event.
        ///    </para>
        /// </summary>
        event DesignerTransactionCloseEventHandler TransactionClosed;

        /// <summary>
        /// <para>Adds an event handler for the <see cref='System.ComponentModel.Design.IDesignerHost.TransactionClosing'/> event.</para>
        /// </summary>
        event DesignerTransactionCloseEventHandler TransactionClosing;

        /// <summary>
        /// <para>Adds an event handler for the <see cref='System.ComponentModel.Design.IDesignerHost.TransactionOpened'/> event.</para>
        /// </summary>
        event EventHandler TransactionOpened;

        /// <summary>
        ///    <para>
        ///       Adds an event handler for the <see cref='System.ComponentModel.Design.IDesignerHost.TransactionOpening'/> event.
        ///    </para>
        /// </summary>
        event EventHandler TransactionOpening;

        /// <summary>
        ///    <para>
        ///       Activates the designer that this host is hosting.
        ///    </para>
        /// </summary>
        void Activate();

        /// <summary>
        ///    <para>
        ///       Creates a component of the specified class type.
        ///    </para>
        /// </summary>
        IComponent CreateComponent(Type componentClass);

        /// <summary>
        ///    <para>
        ///       Creates a component of the given class type and name and places it into the designer container.
        ///    </para>
        /// </summary>
        IComponent CreateComponent(Type componentClass, string name);

        /// <summary>
        /// <para>
        ///     Lengthy operations that involve multiple components may raise many events.  These events
        ///     may cause other side-effects, such as flicker or performance degradation.  When operating
        ///     on multiple components at one time, or setting multiple properties on a single component,
        ///     you should encompass these changes inside a transaction.  Transactions are used
        ///     to improve performance and reduce flicker.  Slow operations can listen to 
        ///     transaction events and only do work when the transaction completes.
        /// </para>
        /// </summary>
        DesignerTransaction CreateTransaction();

        /// <summary>
        /// <para>
        ///     Lengthy operations that involve multiple components may raise many events.  These events
        ///     may cause other side-effects, such as flicker or performance degradation.  When operating
        ///     on multiple components at one time, or setting multiple properties on a single component,
        ///     you should encompass these changes inside a transaction.  Transactions are used
        ///     to improve performance and reduce flicker.  Slow operations can listen to 
        ///     transaction events and only do work when the transaction completes.
        /// </para>
        /// </summary>
        DesignerTransaction CreateTransaction(string description);

        /// <summary>
        ///    <para>
        ///       Destroys the given component, removing it from the design container.
        ///    </para>
        /// </summary>
        void DestroyComponent(IComponent component);

        /// <summary>
        ///    <para>
        ///       Gets the designer instance for the specified component.
        ///    </para>
        /// </summary>
        IDesigner GetDesigner(IComponent component);

        /// <summary>
        ///    <para>
        ///       Gets the type instance for the specified fully qualified type name <paramref name="TypeName"/>.
        ///    </para>
        /// </summary>
        Type GetType(string typeName);
    }
}

