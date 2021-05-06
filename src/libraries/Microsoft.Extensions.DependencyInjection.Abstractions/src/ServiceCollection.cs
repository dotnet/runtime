// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Default implementation of <see cref="IServiceCollection"/>.
    /// </summary>
    public class ServiceCollection : IServiceCollection
    {
        private readonly List<ServiceDescriptor> _descriptors = new List<ServiceDescriptor>();
        private readonly Dictionary<Type, List<ServiceDescriptor>> _serviceTypes = new Dictionary<Type, List<ServiceDescriptor>>();

        /// <inheritdoc />
        public int Count => _descriptors.Count;

        /// <inheritdoc />
        public bool IsReadOnly => false;

        /// <inheritdoc />
        public ServiceDescriptor this[int index]
        {
            get
            {
                return _descriptors[index];
            }
            set
            {
                ServiceDescriptor previous = _descriptors[index];

                if (_serviceTypes.TryGetValue(previous.ServiceType, out List<ServiceDescriptor> items))
                {
                    items.Remove(previous);
                }

                // Add the new entry to the map
                AddToServiceTypeMap(value);

                _descriptors[index] = value;
            }
        }

        /// <inheritdoc />
        public void Clear()
        {
            _serviceTypes.Clear();
            _descriptors.Clear();
        }

        /// <inheritdoc />
        public bool Contains(ServiceDescriptor item)
        {
            return _serviceTypes.TryGetValue(item.ServiceType, out List<ServiceDescriptor> items) &&
                   items.Contains(item);
        }

        /// <inheritdoc />
        public void CopyTo(ServiceDescriptor[] array, int arrayIndex)
        {
            _descriptors.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc />
        public bool Remove(ServiceDescriptor item)
        {
            if (!_serviceTypes.TryGetValue(item.ServiceType, out List<ServiceDescriptor> items))
            {
                return false;
            }

            items.Remove(item);
            return _descriptors.Remove(item);
        }

        /// <inheritdoc />
        public IEnumerator<ServiceDescriptor> GetEnumerator()
        {
            return _descriptors.GetEnumerator();
        }

        void ICollection<ServiceDescriptor>.Add(ServiceDescriptor item)
        {
            AddToServiceTypeMap(item);

            _descriptors.Add(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc />
        public int IndexOf(ServiceDescriptor item)
        {
            return _descriptors.IndexOf(item);
        }

        /// <inheritdoc />
        public void Insert(int index, ServiceDescriptor item)
        {
            // In theory, we should preserve the relative order here but it's expensive to find that out
            // and we handle operations that rely on the relative order already
            AddToServiceTypeMap(item);

            _descriptors.Insert(index, item);
        }

        /// <inheritdoc />
        public void RemoveAt(int index)
        {
            // This will throw if the index is out of range
            ServiceDescriptor item = _descriptors[index];
            if (_serviceTypes.TryGetValue(item.ServiceType, out List<ServiceDescriptor> items))
            {
                items.Remove(item);
            }

            _descriptors.RemoveAt(index);
        }

        internal bool TryAdd(ServiceDescriptor item)
        {
            if (!_serviceTypes.TryGetValue(item.ServiceType, out List<ServiceDescriptor> items))
            {
                _serviceTypes[item.ServiceType] = new List<ServiceDescriptor> { item };
                _descriptors.Add(item);
                return true;
            }
            return false;
        }

        internal bool TryAddEnumerable(ServiceDescriptor item, Type implementationType)
        {
            // Fast path, no service type, then add it
            if (!_serviceTypes.TryGetValue(item.ServiceType, out List<ServiceDescriptor> items))
            {
                _serviceTypes[item.ServiceType] = new List<ServiceDescriptor> { item };
                _descriptors.Add(item);
                return true;
            }

            foreach (ServiceDescriptor d in items)
            {
                // Exact type and implementation already added
                if (d.GetImplementationType() == implementationType)
                {
                    return false;
                }
            }

            // Didn't find a match, so add it
            items.Add(item);
            _descriptors.Add(item);
            return true;
        }

        internal bool TryReplace(ServiceDescriptor item)
        {
            // Fast path, if it's not there, add it
            if (!_serviceTypes.TryGetValue(item.ServiceType, out List<ServiceDescriptor> items))
            {
                _serviceTypes[item.ServiceType] = new List<ServiceDescriptor> { item };
                _descriptors.Add(item);
                return false;
            }

            // This is still O(N)
            for (int i = 0; i < _descriptors.Count; i++)
            {
                ServiceDescriptor descriptor = _descriptors[i];
                if (descriptor.ServiceType == item.ServiceType)
                {
                    // Remove the matching item
                    items.Remove(descriptor);

                    // Remove the first occurrence of the descriptor with matching service type
                    _descriptors.RemoveAt(i);
                    break;
                }
            }

            items.Add(item);
            _descriptors.Add(item);
            return true;
        }

        internal void RemoveAll(Type serviceType)
        {
            if (_serviceTypes.Remove(serviceType))
            {
                for (int i = _descriptors.Count - 1; i >= 0; i--)
                {
                    ServiceDescriptor descriptor = _descriptors[i];
                    if (descriptor.ServiceType == serviceType)
                    {
                        _descriptors.RemoveAt(i);
                    }
                }
            }
        }

        private void AddToServiceTypeMap(ServiceDescriptor item)
        {
            if (!_serviceTypes.TryGetValue(item.ServiceType, out List<ServiceDescriptor> items))
            {
                _serviceTypes[item.ServiceType] = new List<ServiceDescriptor> { item };
            }
            else
            {
                items.Add(item);
            }
        }
    }
}
