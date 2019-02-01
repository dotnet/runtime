// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Design
{
    /// <summary>
    /// Provides methods to adjust the configuration of and retrieve
    /// information about the services and behavior of a designer.
    /// </summary>
    public interface IDesignerHost : IServiceContainer
    {
        /// <summary>
        /// Gets or sets a value indicating whether the designer host
        /// is currently loading the document.
        /// </summary>
        bool Loading { get; }

        /// <summary>
        /// Gets a value indicating whether the designer host is currently in a transaction.
        /// </summary>
        bool InTransaction { get; }

        /// <summary>
        /// Gets the container for this designer host.
        /// </summary>
        IContainer Container { get; }

        /// <summary>
        /// Gets the instance of the base class used as the base class for the current design.
        /// </summary>
        IComponent RootComponent { get; }

        /// <summary>
        /// Gets the fully qualified name of the class that is being designed.
        /// </summary>
        string RootComponentClassName { get; }

        /// <summary>
        /// Gets the description of the current transaction.
        /// </summary>
        string TransactionDescription { get; }

        /// <summary>
        /// Adds an event handler for the <see cref='System.ComponentModel.Design.IDesignerHost.Activated'/> event.
        /// </summary>
        event EventHandler Activated;

        /// <summary>
        /// Adds an event handler for the <see cref='System.ComponentModel.Design.IDesignerHost.Deactivated'/> event.
        /// </summary>
        event EventHandler Deactivated;

        /// <summary>
        /// Adds an event handler for the <see cref='System.ComponentModel.Design.IDesignerHost.LoadComplete'/> event.
        /// </summary>
        event EventHandler LoadComplete;

        /// <summary>
        /// Adds an event handler for the <see cref='System.ComponentModel.Design.IDesignerHost.TransactionClosed'/> event.
        /// </summary>
        event DesignerTransactionCloseEventHandler TransactionClosed;

        /// <summary>
        /// Adds an event handler for the <see cref='System.ComponentModel.Design.IDesignerHost.TransactionClosing'/> event.
        /// </summary>
        event DesignerTransactionCloseEventHandler TransactionClosing;

        /// <summary>
        /// Adds an event handler for the <see cref='System.ComponentModel.Design.IDesignerHost.TransactionOpened'/> event.
        /// </summary>
        event EventHandler TransactionOpened;

        /// <summary>
        /// Adds an event handler for the <see cref='System.ComponentModel.Design.IDesignerHost.TransactionOpening'/> event.
        /// </summary>
        event EventHandler TransactionOpening;

        /// <summary>
        /// Activates the designer that this host is hosting.
        /// </summary>
        void Activate();

        /// <summary>
        /// Creates a component of the specified class type.
        /// </summary>
        IComponent CreateComponent(Type componentClass);

        /// <summary>
        /// Creates a component of the given class type and name and places it into the designer container.
        /// </summary>
        IComponent CreateComponent(Type componentClass, string name);

        /// <summary>
        /// Lengthy operations that involve multiple components may raise many events. These events
        /// may cause other side-effects, such as flicker or performance degradation. When operating
        /// on multiple components at one time, or setting multiple properties on a single component,
        /// you should encompass these changes inside a transaction. Transactions are used
        /// to improve performance and reduce flicker. Slow operations can listen to 
        /// transaction events and only do work when the transaction completes.
        /// </summary>
        DesignerTransaction CreateTransaction();

        /// <summary>
        /// Lengthy operations that involve multiple components may raise many events. These events
        /// may cause other side-effects, such as flicker or performance degradation. When operating
        /// on multiple components at one time, or setting multiple properties on a single component,
        /// you should encompass these changes inside a transaction. Transactions are used
        /// to improve performance and reduce flicker. Slow operations can listen to 
        /// transaction events and only do work when the transaction completes.
        /// </summary>
        DesignerTransaction CreateTransaction(string description);

        /// <summary>
        /// Destroys the given component, removing it from the design container.
        /// </summary>
        void DestroyComponent(IComponent component);

        /// <summary>
        /// Gets the designer instance for the specified component.
        /// </summary>
        IDesigner GetDesigner(IComponent component);

        /// <summary>
        /// Gets the type instance for the specified fully qualified type name <paramref name="typeName"/>.
        /// </summary>
        Type GetType(string typeName);
    }
}
