// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Security.Permissions;
using System.Runtime.InteropServices;

namespace System.ComponentModel
{
    /// <summary>
    /// <para>Provides the base implementation for <see cref='System.ComponentModel.IComponent'/>,
    ///    which is the base class for all components in Win Forms.</para>
    /// </summary>
    [
        Designer("System.Windows.Forms.Design.ComponentDocumentDesigner, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", typeof(IRootDesigner)),
        DesignerCategory("Component"),
        TypeConverter(typeof(ComponentConverter))
    ]
    public class MarshalByValueComponent : IComponent, IServiceProvider
    {
        /// <summary>
        ///    <para>Static hask key for the Disposed event. This field is read-only.</para>
        /// </summary>
        private static readonly object s_eventDisposed = new object();

        private ISite _site;
        private EventHandlerList _events;

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.MarshalByValueComponent'/> class.</para>
        /// </summary>
        public MarshalByValueComponent()
        {
        }

        ~MarshalByValueComponent()
        {
            Dispose(false);
        }

        /// <summary>
        ///    <para>Adds an event handler to listen to the Disposed event on the component.</para>
        /// </summary>
        public event EventHandler Disposed
        {
            add
            {
                Events.AddHandler(s_eventDisposed, value);
            }
            remove
            {
                Events.RemoveHandler(s_eventDisposed, value);
            }
        }

        /// <summary>
        ///    <para>Gets the list of event handlers that are attached to this component.</para>
        /// </summary>
        protected EventHandlerList Events => _events ?? (_events = new EventHandlerList());

        /// <summary>
        ///    <para>Gets or sets the site of the component.</para>
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual ISite Site
        {
            get { return _site; }
            set { _site = value; }
        }

        /// <summary>
        ///    <para>Disposes of the resources (other than memory) used by the component.</para>
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed")]
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
                    ((EventHandler) _events?[s_eventDisposed])?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        ///    <para>Gets the container for the component.</para>
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual IContainer Container => _site?.Container;

        /// <summary>
        /// <para>Gets the implementer of the <see cref='System.IServiceProvider'/>.</para>
        /// </summary>
        public virtual object GetService(Type service)
        {
            return _site?.GetService(service);
        }


        /// <summary>
        ///    <para>Gets a value indicating whether the component is currently in design mode.</para>
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual bool DesignMode
        {
            get
            {
                ISite s = _site;
                return s?.DesignMode ?? false;
            }
        }
        /// <internalonly/>
        /// <summary>
        ///    <para>
        ///       Returns a <see cref='System.String'/> containing the name of the <see cref='System.ComponentModel.Component'/> , if any. This method should not be
        ///       overridden. For
        ///       internal use only.
        ///    </para>
        /// </summary>
        public override String ToString()
        {
            ISite s = _site;

            if (s != null)
                return s.Name + " [" + GetType().FullName + "]";
            else
                return GetType().FullName;
        }
    }
}
