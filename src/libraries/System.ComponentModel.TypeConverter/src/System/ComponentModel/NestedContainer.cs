// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.ComponentModel
{
    /// <summary>
    /// A nested container is a container that is owned by another component. Nested
    /// containers can be found by querying a component site's services for NestedConainter.
    /// Nested containers are a useful tool to establish owner relationships among components.
    /// All components within a nested container are named with the owning component's name
    /// as a prefix.
    /// </summary>
    public class NestedContainer : Container, INestedContainer
    {
        /// <summary>
        /// Creates a new NestedContainer.
        /// </summary>
        public NestedContainer(IComponent owner)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Owner.Disposed += new EventHandler(OnOwnerDisposed);
        }

        /// <summary>
        /// The component that owns this nested container.
        /// </summary>
        public IComponent Owner { get; }

        /// <summary>
        /// Retrieves the name of the owning component. This may be overridden to
        /// provide a custom owner name. The default searches the owner's site for
        /// INestedSite and calls FullName, or ISite.Name if there is no nested site.
        /// If neither is available, this returns null.
        /// </summary>
        protected virtual string OwnerName
        {
            get
            {
                string ownerName = null;
                if (Owner != null && Owner.Site != null)
                {
                    if (Owner.Site is INestedSite nestedOwnerSite)
                    {
                        ownerName = nestedOwnerSite.FullName;
                    }
                    else
                    {
                        ownerName = Owner.Site.Name;
                    }
                }

                return ownerName;
            }
        }

        /// <summary>
        /// Creates a site for the component within the container.
        /// </summary>
        protected override ISite CreateSite(IComponent component, string name)
        {
            if (component == null)
            {
                throw new ArgumentNullException(nameof(component));
            }
            return new Site(component, this, name);
        }

        /// <summary>
        /// Override of Container's dispose.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Owner.Disposed -= new EventHandler(OnOwnerDisposed);
            }
            base.Dispose(disposing);
        }

        protected override object GetService(Type service)
        {
            if (service == typeof(INestedContainer))
            {
                return this;
            }
            else
            {
                return base.GetService(service);
            }
        }

        /// <summary>
        /// Called when our owning component is destroyed.
        /// </summary>
        private void OnOwnerDisposed(object sender, EventArgs e) => Dispose();

        /// <summary>
        /// Simple site implementation. We do some special processing to name the site, but
        /// that's about it.
        /// </summary>
        private sealed class Site : INestedSite
        {
            private string _name;

            internal Site(IComponent component, NestedContainer container, string name)
            {
                Component = component;
                Container = container;
                _name = name;
            }

            // The component sited by this component site.
            public IComponent Component { get; }

            // The container in which the component is sited.
            public IContainer Container { get; }

            public object GetService(Type service)
            {
                return ((service == typeof(ISite)) ? this : ((NestedContainer)Container).GetService(service));
            }

            // Indicates whether the component is in design mode.
            public bool DesignMode
            {
                get
                {
                    IComponent owner = ((NestedContainer)Container).Owner;
                    if (owner != null && owner.Site != null)
                    {
                        return owner.Site.DesignMode;
                    }
                    return false;
                }
            }

            public string FullName
            {
                get
                {
                    if (_name != null)
                    {
                        string ownerName = ((NestedContainer)Container).OwnerName;
                        string childName = _name;
                        if (ownerName != null)
                        {
                            childName = ownerName + "." + childName;
                        }

                        return childName;
                    }

                    return _name;
                }
            }

            // The name of the component.
            public string Name
            {
                get => _name;
                [RequiresUnreferencedCode("The Type of components in the container cannot be statically discovered to validate the name.")]
                set
                {
                    if (value == null || _name == null || !value.Equals(_name))
                    {
                        ((NestedContainer)Container).ValidateName(Component, value);
                        _name = value;
                    }
                }
            }
        }
    }
}
