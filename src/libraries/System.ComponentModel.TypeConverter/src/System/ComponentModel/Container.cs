// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel
{
    /// <summary>
    /// Encapsulates zero or more components.
    /// </summary>
    public class Container : IContainer
    {
        private ISite?[]? _sites;
        private int _siteCount;
        private ComponentCollection? _components;
        private ContainerFilterService? _filter;
        private bool _checkedFilter;

        private readonly object _syncObj = new object();

        ~Container() => Dispose(false);

        /// <summary>
        /// Adds the specified component to the <see cref='System.ComponentModel.Container'/>
        /// The component is unnamed.
        /// </summary>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "No name is provided.")]
        public virtual void Add(IComponent? component) => Add(component, null);

        // Adds a component to the container.
        /// <summary>
        /// Adds the specified component to the <see cref='System.ComponentModel.Container'/> and assigns
        /// a name to it.
        /// </summary>
        [RequiresUnreferencedCode("The Type of components in the container cannot be statically discovered to validate the name.")]
        public virtual void Add(IComponent? component, string? name)
        {
            lock (_syncObj)
            {
                if (component == null)
                {
                    return;
                }

                ISite? site = component.Site;
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
                    // Validate that new components have either a null name or a unique one.
                    ValidateName(component, name);

                    if (_sites.Length == _siteCount)
                    {
                        ISite[] newSites = new ISite[_siteCount * 2];
                        Array.Copy(_sites, newSites, _siteCount);
                        _sites = newSites;
                    }
                }

                site?.Container!.Remove(component);

                ISite newSite = CreateSite(component, name);
                _sites[_siteCount++] = newSite;
                component.Site = newSite;
                _components = null;
            }
        }

        /// <summary>
        /// Creates a Site <see cref='System.ComponentModel.ISite'/> for the given <see cref='System.ComponentModel.IComponent'/>
        /// and assigns the given name to the site.
        /// </summary>
        protected virtual ISite CreateSite(IComponent component, string? name)
        {
            return new Site(component, this, name);
        }

        /// <summary>
        /// Disposes of the container. A call to the Dispose method indicates that
        /// the user of the container has no further need for it.
        ///
        /// The implementation of Dispose must:
        ///
        /// (1) Remove any references the container is holding to other components.
        /// This is typically accomplished by assigning null to any fields that
        /// contain references to other components.
        ///
        /// (2) Release any system resources that are associated with the container,
        /// such as file handles, window handles, or database connections.
        ///
        /// (3) Dispose of child components by calling the Dispose method of each.
        ///
        /// Ideally, a call to Dispose will revert a container to the state it was
        /// in immediately after it was created. However, this is not a requirement.
        /// Following a call to its Dispose method, a container is permitted to raise
        /// exceptions for operations that cannot meaningfully be performed.
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
                        ISite site = _sites![--_siteCount]!;
                        site.Component.Site = null;
                        site.Component.Dispose();
                    }
                    _sites = null;
                    _components = null;
                }
            }
        }

        protected virtual object? GetService(Type service) => service == typeof(IContainer) ? this : null;

        /// <summary>
        /// Gets all the components in the <see cref='System.ComponentModel.Container'/>.
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
                            result[i] = _sites![i]!.Component;
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
                        if (filteredComponents != null)
                        {
                            _components = filteredComponents;
                        }
                    }

                    return _components;
                }
            }
        }

        /// <summary>
        /// Removes a component from the <see cref='System.ComponentModel.Container'/>.
        /// </summary>
        public virtual void Remove(IComponent? component) => Remove(component, false);

        private void Remove(IComponent? component, bool preserveSite)
        {
            lock (_syncObj)
            {
                ISite? site = component?.Site;
                if (site == null || site.Container != this)
                {
                    return;
                }

                if (!preserveSite)
                {
                    component!.Site = null;
                }

                for (int i = 0; i < _siteCount; i++)
                {
                    if (_sites![i] == site)
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

        protected void RemoveWithoutUnsiting(IComponent? component) => Remove(component, true);

        /// <summary>
        /// Validates that the given name is valid for a component. The default implementation
        /// verifies that name is either null or unique compared to the names of other
        /// components in the container.
        /// </summary>
        [RequiresUnreferencedCode("The Type of components in the container cannot be statically discovered.")]
        protected virtual void ValidateName(IComponent component, string? name)
        {
            if (component == null)
            {
                throw new ArgumentNullException(nameof(component));
            }

            if (name != null)
            {
                for (int i = 0; i < Math.Min(_siteCount, _sites!.Length); i++)
                {
                    ISite? s = _sites[i];

                    if (s?.Name != null && string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase) && s.Component != component)
                    {
                        InheritanceAttribute inheritanceAttribute = (InheritanceAttribute)TypeDescriptor.GetAttributes(s.Component)[typeof(InheritanceAttribute)]!;
                        if (inheritanceAttribute.InheritanceLevel != InheritanceLevel.InheritedReadOnly)
                        {
                            throw new ArgumentException(SR.Format(SR.DuplicateComponentName, name));
                        }
                    }
                }
            }
        }

        private sealed class Site : ISite
        {
            private string? _name;

            internal Site(IComponent component, Container container, string? name)
            {
                Component = component;
                Container = container;
                _name = name;
            }

            /// <summary>
            /// The component sited by this component site.
            /// </summary>
            public IComponent Component { get; }

            /// <summary>
            /// The container in which the component is sited.
            /// </summary>
            public IContainer Container { get; }

            public object? GetService(Type service)
            {
                return ((service == typeof(ISite)) ? this : ((Container)Container).GetService(service));
            }

            /// <summary>
            /// Indicates whether the component is in design mode.
            /// </summary>
            public bool DesignMode => false;

            /// <summary>
            /// The name of the component.
            /// </summary>
            public string? Name
            {
                get => _name;
                [RequiresUnreferencedCode("The Type of components in the container cannot be statically discovered to validate the name.")]
                set
                {
                    if (value == null || _name == null || !value.Equals(_name))
                    {
                        ((Container)Container).ValidateName(Component, value);
                        _name = value;
                    }
                }
            }
        }
    }
}
