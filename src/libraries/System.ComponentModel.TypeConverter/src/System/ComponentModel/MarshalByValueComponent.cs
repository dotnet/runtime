// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel
{
    /// <summary>
    /// Provides the base implementation for <see cref='System.ComponentModel.IComponent'/>,
    /// which is the base class for all components in Win Forms.
    /// </summary>
    [Designer("System.Windows.Forms.Design.ComponentDocumentDesigner, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
              "System.ComponentModel.Design.IRootDesigner, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    [DesignerCategory("Component")]
    [TypeConverter(typeof(ComponentConverter))]
    public class MarshalByValueComponent : IComponent, IServiceProvider
    {
        /// <summary>
        /// Static hask key for the Disposed event. This field is read-only.
        /// </summary>
        private static readonly object s_eventDisposed = new object();

        private ISite? _site;
        private EventHandlerList? _events;

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.MarshalByValueComponent'/> class.
        /// </summary>
        public MarshalByValueComponent()
        {
        }

        ~MarshalByValueComponent() => Dispose(false);

        /// <summary>
        /// Adds an event handler to listen to the Disposed event on the component.
        /// </summary>
        public event EventHandler? Disposed
        {
            add => Events.AddHandler(s_eventDisposed, value);
            remove => Events.RemoveHandler(s_eventDisposed, value);
        }

        /// <summary>
        /// Gets the list of event handlers that are attached to this component.
        /// </summary>
        protected EventHandlerList Events => _events ?? (_events = new EventHandlerList());

        /// <summary>
        /// Gets or sets the site of the component.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual ISite? Site
        {
            get => _site;
            set => _site = value;
        }

        /// <summary>
        /// Disposes of the resources (other than memory) used by the component.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///
        /// Disposes all the resources associated with this component.
        /// If disposing is false then you must never touch any other
        /// managed objects, as they may already be finalized. When
        /// in this state you should dispose any native resources
        /// that you have a reference to.
        ///
        ///
        /// When disposing is true then you should dispose all data
        /// and objects you have references to. The normal implementation
        /// of this method would look something like:
        ///
        /// <code>
        /// public void Dispose() {
        /// Dispose(true);
        /// GC.SuppressFinalize(this);
        /// }
        ///
        /// protected virtual void Dispose(bool disposing) {
        /// if (disposing) {
        ///   if (myobject != null) {
        ///       myobject.Dispose();
        ///       myobject = null;
        ///   }
        /// }
        /// if (myhandle != IntPtr.Zero) {
        ///   NativeMethods.Release(myhandle);
        ///   myhandle = IntPtr.Zero;
        /// }
        /// }
        ///
        /// ~MyClass() {
        /// Dispose(false);
        /// }
        /// </code>
        ///
        /// For base classes, you should never override the Finalizer (~Class in C#)
        /// or the Dispose method that takes no arguments, rather you should
        /// always override the Dispose method that takes a bool.
        ///
        /// <code>
        /// protected override void Dispose(bool disposing) {
        /// if (disposing) {
        ///   if (myobject != null) {
        ///       myobject.Dispose();
        ///       myobject = null;
        ///   }
        /// }
        /// if (myhandle != IntPtr.Zero) {
        ///   NativeMethods.Release(myhandle);
        ///   myhandle = IntPtr.Zero;
        /// }
        /// base.Dispose(disposing);
        /// }
        /// </code>
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (this)
                {
                    _site?.Container?.Remove(this);
                    ((EventHandler?)_events?[s_eventDisposed])?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Gets the container for the component.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual IContainer? Container => _site?.Container;

        /// <summary>
        /// Gets the implementer of the <see cref='System.IServiceProvider'/>.
        /// </summary>
        public virtual object? GetService(Type service) => _site?.GetService(service);

        /// <summary>
        /// Gets a value indicating whether the component is currently in design mode.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual bool DesignMode => _site?.DesignMode ?? false;

        /// <summary>
        /// Returns a <see cref='string'/> containing the name of the <see cref='System.ComponentModel.Component'/> , if any. This method should not be
        /// overridden. For
        /// internal use only.
        /// </summary>
        public override string? ToString()
        {
            ISite? s = _site;

            if (s != null)
                return s.Name + " [" + GetType().FullName + "]";
            else
                return GetType().FullName;
        }
    }
}
