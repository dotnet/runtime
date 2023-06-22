// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    internal sealed class DefaultHttpClientBuilderServiceCollection : IServiceCollection
    {
        private readonly IServiceCollection _services;
        private ServiceDescriptor? _lastAdded;

        public DefaultHttpClientBuilderServiceCollection(IServiceCollection services)
        {
            _services = services;
        }

        public void Add(ServiceDescriptor item)
        {
            // Insert IConfigureOptions<T> services into the collection before other definitions.
            // This ensures they run first, apply configuration, then named clients run afterwards.
            if (item.ServiceType.IsGenericType && item.ServiceType.GetGenericTypeDefinition() == typeof(IConfigureOptions<>))
            {
                var insertIndex = 0;

                // If configuration has already been added, additional configuration should come after.
                // This is done to preserve the order that default configuration is run.
                if (_lastAdded is not null && _services.IndexOf(_lastAdded) is var index && index != -1)
                {
                    insertIndex = index + 1;
                }

                _services.Insert(insertIndex, item);
                _lastAdded = item;
                return;
            }

            _services.Add(item);
        }

        public ServiceDescriptor this[int index]
        {
            get => _services[index];
            set => _services[index] = value;
        }
        public int Count => _services.Count;
        public bool IsReadOnly => _services.IsReadOnly;
        public void Clear() => _services.Clear();
        public bool Contains(ServiceDescriptor item) => _services.Contains(item);
        public void CopyTo(ServiceDescriptor[] array, int arrayIndex) => _services.CopyTo(array, arrayIndex);
        public IEnumerator<ServiceDescriptor> GetEnumerator() => _services.GetEnumerator();
        public int IndexOf(ServiceDescriptor item) => _services.IndexOf(item);
        public void Insert(int index, ServiceDescriptor item) => _services.Insert(index, item);
        public bool Remove(ServiceDescriptor item) => _services.Remove(item);
        public void RemoveAt(int index) => _services.RemoveAt(index);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
