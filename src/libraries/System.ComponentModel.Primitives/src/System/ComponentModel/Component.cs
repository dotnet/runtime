// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>Provides the default implementation for the 
    ///    <see cref='System.ComponentModel.IComponent'/>
    ///    interface and enables object-sharing between applications.</para>
    /// </summary>
    [DesignerCategory("Component")]
    public class Component : MarshalByRefObject, IComponent
    {
        /// <summary>
        ///    <para>Static hask key for the Disposed event. This field is read-only.</para>
        /// </summary>
        private static readonly object s_eventDisposed = new object();

        private ISite _site;
        private EventHandlerList _events;

        ~Component() => Dispose(false);

        /// <summary>
        ///     This property returns true if the component is in a mode that supports
        ///     raising events.  By default, components always support raising their events
        ///     and therefore this method always returns true.  You can override this method
        ///     in a deriving class and change it to return false when needed.  if the return
        ///     value of this method is false, the EventList collection returned by the Events
        ///     property will always return null for an event.  Events can still be added and
        ///     removed from the collection, but retrieving them through the collection's Item
        ///     property will always return null.
        /// </summary>
        protected virtual bool CanRaiseEvents => true;

        /// <summary>
        ///     Internal API that allows the event handler list class to access the
        ///     CanRaiseEvents property.
        /// </summary>
        internal bool CanRaiseEventsInternal => CanRaiseEvents;

        /// <summary>
        ///    <para>Adds an event handler to listen to the Disposed event on the component.</para>
        /// </summary>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public event EventHandler Disposed
        {
            add => Events.AddHandler(s_eventDisposed, value);
            remove => Events.RemoveHandler(s_eventDisposed, value);
        }

        /// <summary>
        ///    <para>Gets the list of event handlers that are attached to this component.</para>
        /// </summary>
        protected EventHandlerList Events => _events ?? (_events = new EventHandlerList(this));

        /// <summary>
        ///    <para>
        ///       Gets or sets the site of the <see cref='System.ComponentModel.Component'/>
        ///       .
        ///    </para>
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual ISite Site
        {
            get => _site;
            set => _site = value;
        }

        /// <summary>
        ///    <para>
        ///       Disposes of the <see cref='System.ComponentModel.Component'/>
        ///       .
        ///    </para>
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///    <para>
        ///    Disposes all the resources associated with this component.
        ///    If disposing is false then you must never touch any other
        ///    managed objects, as they may already be finalized. When
        ///    in this state you should dispose any native resources
        ///    that you have a reference to.
        ///    </para>
        ///    <para>
        ///    When disposing is true then you should dispose all data
        ///    and objects you have references to. The normal implementation
        ///    of this method would look something like:
        ///    </para>
        ///    <code>
        ///    public void Dispose() {
        ///        Dispose(true);
        ///        GC.SuppressFinalize(this);
        ///    }
        ///
        ///    protected virtual void Dispose(bool disposing) {
        ///        if (disposing) {
        ///            if (myobject != null) {
        ///                myobject.Dispose();
        ///                myobject = null;
        ///            }
        ///        }
        ///        if (myhandle != IntPtr.Zero) {
        ///            NativeMethods.Release(myhandle);
        ///            myhandle = IntPtr.Zero;
        ///        }
        ///    }
        ///
        ///    ~MyClass() {
        ///        Dispose(false);
        ///    }
        ///    </code>
        ///    <para>
        ///    For base classes, you should never override the Finalizer (~Class in C#)
        ///    or the Dispose method that takes no arguments, rather you should
        ///    always override the Dispose method that takes a bool. 
        ///    </para>
        ///    <code>
        ///    protected override void Dispose(bool disposing) {
        ///        if (disposing) {
        ///            if (myobject != null) {
        ///                myobject.Dispose();
        ///                myobject = null;
        ///            }
        ///        }
        ///        if (myhandle != IntPtr.Zero) {
        ///            NativeMethods.Release(myhandle);
        ///            myhandle = IntPtr.Zero;
        ///        }
        ///        base.Dispose(disposing);
        ///    }
        ///    </code>
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (this)
                {
                    _site?.Container?.Remove(this);
                    if (_events != null)
                    {
                        ((EventHandler)_events[s_eventDisposed])?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
        }

        // Returns the component's container.
        //
        /// <summary>
        ///    <para>
        ///       Returns the <see cref='System.ComponentModel.IContainer'/>
        ///       that contains the <see cref='System.ComponentModel.Component'/>.
        ///    </para>
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IContainer Container => _site?.Container;

        /// <summary>
        ///    <para>
        ///       Returns an object representing a service provided by
        ///       the <see cref='System.ComponentModel.Component'/>.
        ///    </para>
        /// </summary>
        protected virtual object GetService(Type service) => _site?.GetService(service);

        /// <summary>
        ///    <para>
        ///       Gets a value indicating whether the <see cref='System.ComponentModel.Component'/>
        ///       is currently in design mode.
        ///    </para>
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        protected bool DesignMode => _site?.DesignMode ?? false;

        /// <internalonly/>
        /// <summary>
        ///    <para>
        ///       Returns a <see cref='System.String'/> containing the name of the <see cref='System.ComponentModel.Component'/> , if any. This method should not be
        ///       overridden. For
        ///       internal use only.
        ///    </para>
        /// </summary>
        public override string ToString()
        {
            ISite s = _site;

            if (s != null)
                return s.Name + " [" + GetType().FullName + "]";
            else
                return GetType().FullName;
        }
    }
}
