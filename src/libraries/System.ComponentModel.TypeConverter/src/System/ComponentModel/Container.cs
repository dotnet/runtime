// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>
    ///       Encapsulates
    ///       zero or more components.
    ///    </para>
    /// </summary>
    public class Container : IContainer
    {
        private ISite[] _sites;
        private int _siteCount;
        private ComponentCollection _components;
        private ContainerFilterService _filter;
        private bool _checkedFilter;

        private object _syncObj = new Object();

        ~Container()
        {
            Dispose(false);
        }

        // Adds a component to the container.
        /// <summary>
        ///    <para>
        ///       Adds the specified component to the <see cref='System.ComponentModel.Container'/>
        ///       . The component is unnamed.
        ///    </para>
        /// </summary>
        public virtual void Add(IComponent component)
        {
            Add(component, null);
        }

        // Adds a component to the container.
        /// <summary>
        ///    <para>
        ///       Adds the specified component to the <see cref='System.ComponentModel.Container'/> and assigns a name to
        ///       it.
        ///    </para>
        /// </summary>
        public virtual void Add(IComponent component, String name)
        {
            lock (_syncObj)
            {
                if (component == null)
                {
                    return;
                }

                ISite site = component.Site;

                if (site != null && site.Container == this)
                {
                    return;
                }

                if (_sites == null)
                {
                    _sites = new ISite[4];
                }
                else
                {
                    // Validate that new components
                    // have either a null name or a unique one.
                    //
                    ValidateName(component, name);

                    if (_sites.Length == _siteCount)
                    {
                        ISite[] newSites = new ISite[_siteCount * 2];
                        Array.Copy(_sites, 0, newSites, 0, _siteCount);
                        _sites = newSites;
                    }
                }

                if (site != null)
                {
                    site.Container.Remove(component);
                }

                ISite newSite = CreateSite(component, name);
                _sites[_siteCount++] = newSite;
                component.Site = newSite;
                _components = null;
            }
        }

        // Creates a site for the component within the container.
        /// <summary>
        /// <para>Creates a Site <see cref='System.ComponentModel.ISite'/> for the given <see cref='System.ComponentModel.IComponent'/>
        /// and assigns the given name to the site.</para>
        /// </summary>
        protected virtual ISite CreateSite(IComponent component, string name)
        {
            return new Site(component, this, name);
        }

        // Disposes of the container.  A call to the Dispose method indicates that
        // the user of the container has no further need for it.
        //
        // The implementation of Dispose must:
        //
        // (1) Remove any references the container is holding to other components.
        //     This is typically accomplished by assigning null to any fields that
        //     contain references to other components.
        //
        // (2) Release any system resources that are associated with the container,
        //     such as file handles, window handles, or database connections.
        //
        // (3) Dispose of child components by calling the Dispose method of each.
        //
        // Ideally, a call to Dispose will revert a container to the state it was
        // in immediately after it was created. However, this is not a requirement.
        // Following a call to its Dispose method, a container is permitted to raise
        // exceptions for operations that cannot meaningfully be performed.
        //
        /// <summary>
        ///    <para>
        ///       Disposes of the <see cref='System.ComponentModel.Container'/>
        ///       .
        ///    </para>
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_syncObj)
                {
                    while (_siteCount > 0)
                    {
                        ISite site = _sites[--_siteCount];
                        site.Component.Site = null;
                        site.Component.Dispose();
                    }
                    _sites = null;
                    _components = null;
                }
            }
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        protected virtual object GetService(Type service)
        {
            return ((service == typeof(IContainer)) ? this : null);
        }

        // The components in the container.
        /// <summary>
        ///    <para>
        ///       Gets all the components in the <see cref='System.ComponentModel.Container'/>
        ///       .
        ///    </para>
        /// </summary>
        public virtual ComponentCollection Components
        {
            get
            {
                lock (_syncObj)
                {
                    if (_components == null)
                    {
                        IComponent[] result = new IComponent[_siteCount];
                        for (int i = 0; i < _siteCount; i++)
                        {
                            result[i] = _sites[i].Component;
                        }
                        _components = new ComponentCollection(result);

                        // At each component add, if we don't yet have a filter, look for one.
                        // Components may add filters.
                        if (_filter == null && _checkedFilter)
                        {
                            _checkedFilter = false;
                        }
                    }

                    if (!_checkedFilter)
                    {
                        _filter = GetService(typeof(ContainerFilterService)) as ContainerFilterService;
                        _checkedFilter = true;
                    }

                    if (_filter != null)
                    {
                        ComponentCollection filteredComponents = _filter.FilterComponents(_components);
                        Debug.Assert(filteredComponents != null, "Incorrect ContainerFilterService implementation.");
                        if (filteredComponents != null)
                        {
                            _components = filteredComponents;
                        }
                    }

                    return _components;
                }
            }
        }

        // Removes a component from the container.
        /// <summary>
        ///    <para>
        ///       Removes a component from the <see cref='System.ComponentModel.Container'/>
        ///       .
        ///    </para>
        /// </summary>
        public virtual void Remove(IComponent component)
        {
            Remove(component, false);
        }

        private void Remove(IComponent component, bool preserveSite)
        {
            lock (_syncObj)
            {
                if (component == null)
                    return;
                ISite site = component.Site;
                if (site == null || site.Container != this)
                    return;
                if (!preserveSite)
                    component.Site = null;
                for (int i = 0; i < _siteCount; i++)
                {
                    if (_sites[i] == site)
                    {
                        _siteCount--;
                        Array.Copy(_sites, i + 1, _sites, i, _siteCount - i);
                        _sites[_siteCount] = null;
                        _components = null;
                        break;
                    }
                }
            }
        }

        protected void RemoveWithoutUnsiting(IComponent component)
        {
            Remove(component, true);
        }

        /// <summary>
        ///     Validates that the given name is valid for a component.  The default implementation
        ///     verifies that name is either null or unique compared to the names of other
        ///     components in the container.
        /// </summary>
        protected virtual void ValidateName(IComponent component, string name)
        {
            if (component == null)
            {
                throw new ArgumentNullException(nameof(component));
            }

            if (name != null)
            {
                for (int i = 0; i < Math.Min(_siteCount, _sites.Length); i++)
                {
                    ISite s = _sites[i];

                    if (s != null && s.Name != null
                            && string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)
                            && s.Component != component)
                    {
                        InheritanceAttribute inheritanceAttribute = (InheritanceAttribute)TypeDescriptor.GetAttributes(s.Component)[typeof(InheritanceAttribute)];
                        if (inheritanceAttribute.InheritanceLevel != InheritanceLevel.InheritedReadOnly)
                        {
                            throw new ArgumentException(SR.Format(SR.DuplicateComponentName, name));
                        }
                    }
                }
            }
        }

        private class Site : ISite
        {
            private IComponent _component;
            private Container _container;
            private String _name;

            internal Site(IComponent component, Container container, String name)
            {
                _component = component;
                _container = container;
                _name = name;
            }

            // The component sited by this component site.
            public IComponent Component
            {
                get
                {
                    return _component;
                }
            }

            // The container in which the component is sited.
            public IContainer Container
            {
                get
                {
                    return _container;
                }
            }

            public Object GetService(Type service)
            {
                return ((service == typeof(ISite)) ? this : _container.GetService(service));
            }


            // Indicates whether the component is in design mode.
            public bool DesignMode
            {
                get
                {
                    return false;
                }
            }

            // The name of the component.
            //
            public String Name
            {
                get { return _name; }
                set
                {
                    if (value == null || _name == null || !value.Equals(_name))
                    {
                        // UNDONE : This is a breaking change.  
                        _container.ValidateName(_component, value);
                        _name = value;
                    }
                }
            }
        }
    }
}
