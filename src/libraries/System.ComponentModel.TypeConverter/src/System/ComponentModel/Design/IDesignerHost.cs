// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Design {
    using System.Diagnostics;
    using System;
    using System.ComponentModel;
    using Microsoft.Win32;
    using System.Reflection;

    /// <devdoc>
    ///    <para>
    ///       Provides methods to adjust the configuration of and retrieve
    ///       information about the services and behavior of a designer.
    ///    </para>
    /// </devdoc>
    [System.Runtime.InteropServices.ComVisible(true)]
    public interface IDesignerHost : IServiceContainer {

        /// <devdoc>
        ///    <para>
        ///       Gets or sets a value indicating whether the designer host
        ///       is currently loading the document.
        ///    </para>
        /// </devdoc>
        bool Loading { get; }

        /// <devdoc>
        ///    <para>Gets a value indicating whether the designer host is currently in a transaction.</para>
        /// </devdoc>
        bool InTransaction { get; }
        
        /// <devdoc>
        ///    <para>
        ///       Gets the container for this designer host.
        ///    </para>
        /// </devdoc>
        IContainer Container { get; }

        /// <devdoc>
        ///    <para>
        ///       Gets the instance of the base class used as the base class for the current design.
        ///    </para>
        /// </devdoc>
        IComponent RootComponent { get; }

        /// <devdoc>
        ///    <para>
        ///       Gets the fully qualified name of the class that is being designed.
        ///    </para>
        /// </devdoc>
        string RootComponentClassName { get; }

        /// <devdoc>
        ///    <para>
        ///       Gets the description of the current transaction.
        ///    </para>
        /// </devdoc>
        string TransactionDescription { get; }

        /// <devdoc>
        ///    <para>
        ///       Adds an event handler for the <see cref='System.ComponentModel.Design.IDesignerHost.Activated'/> event.
        ///    </para>
        /// </devdoc>
        event EventHandler Activated;

        /// <devdoc>
        ///    <para>
        ///       Adds an event handler for the <see cref='System.ComponentModel.Design.IDesignerHost.Deactivated'/> event.
        ///    </para>
        /// </devdoc>
        event EventHandler Deactivated;

        /// <devdoc>
        ///    <para>
        ///       Adds an event handler for the <see cref='System.ComponentModel.Design.IDesignerHost.LoadComplete'/> event.
        ///    </para>
        /// </devdoc>
        event EventHandler LoadComplete;

        /// <devdoc>
        ///    <para>
        ///       Adds an event handler for the <see cref='System.ComponentModel.Design.IDesignerHost.TransactionClosed'/> event.
        ///    </para>
        /// </devdoc>
        event DesignerTransactionCloseEventHandler TransactionClosed;
        
        /// <devdoc>
        /// <para>Adds an event handler for the <see cref='System.ComponentModel.Design.IDesignerHost.TransactionClosing'/> event.</para>
        /// </devdoc>
        event DesignerTransactionCloseEventHandler TransactionClosing;

        /// <devdoc>
        /// <para>Adds an event handler for the <see cref='System.ComponentModel.Design.IDesignerHost.TransactionOpened'/> event.</para>
        /// </devdoc>
        event EventHandler TransactionOpened;

        /// <devdoc>
        ///    <para>
        ///       Adds an event handler for the <see cref='System.ComponentModel.Design.IDesignerHost.TransactionOpening'/> event.
        ///    </para>
        /// </devdoc>
        event EventHandler TransactionOpening;
        
        /// <devdoc>
        ///    <para>
        ///       Activates the designer that this host is hosting.
        ///    </para>
        /// </devdoc>
        void Activate();

        /// <devdoc>
        ///    <para>
        ///       Creates a component of the specified class type.
        ///    </para>
        /// </devdoc>
        IComponent CreateComponent(Type componentClass);

        /// <devdoc>
        ///    <para>
        ///       Creates a component of the given class type and name and places it into the designer container.
        ///    </para>
        /// </devdoc>
        IComponent CreateComponent(Type componentClass, string name);

        /// <devdoc>
        /// <para>
        ///     Lengthy operations that involve multiple components may raise many events.  These events
        ///     may cause other side-effects, such as flicker or performance degradation.  When operating
        ///     on multiple components at one time, or setting multiple properties on a single component,
        ///     you should encompass these changes inside a transaction.  Transactions are used
        ///     to improve performance and reduce flicker.  Slow operations can listen to 
        ///     transaction events and only do work when the transaction completes.
        /// </para>
        /// </devdoc>
        DesignerTransaction CreateTransaction();

        /// <devdoc>
        /// <para>
        ///     Lengthy operations that involve multiple components may raise many events.  These events
        ///     may cause other side-effects, such as flicker or performance degradation.  When operating
        ///     on multiple components at one time, or setting multiple properties on a single component,
        ///     you should encompass these changes inside a transaction.  Transactions are used
        ///     to improve performance and reduce flicker.  Slow operations can listen to 
        ///     transaction events and only do work when the transaction completes.
        /// </para>
        /// </devdoc>
        DesignerTransaction CreateTransaction(string description);

        /// <devdoc>
        ///    <para>
        ///       Destroys the given component, removing it from the design container.
        ///    </para>
        /// </devdoc>
        void DestroyComponent(IComponent component);

        /// <devdoc>
        ///    <para>
        ///       Gets the designer instance for the specified component.
        ///    </para>
        /// </devdoc>
        IDesigner GetDesigner(IComponent component);

        /// <devdoc>
        ///    <para>
        ///       Gets the type instance for the specified fully qualified type name <paramref name="TypeName"/>.
        ///    </para>
        /// </devdoc>
        Type GetType(string typeName);
    }
}

