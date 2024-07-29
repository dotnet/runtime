// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    internal sealed class DefaultHttpClientBuilderServiceCollection : IServiceCollection
    {
        private readonly IServiceCollection _services;
        private readonly bool _isDefault;
        private readonly DefaultHttpClientConfigurationTracker _tracker;

        public DefaultHttpClientBuilderServiceCollection(IServiceCollection services, bool isDefault, DefaultHttpClientConfigurationTracker tracker)
        {
            _services = services;
            _isDefault = isDefault;
            _tracker = tracker;
        }

        public void Add(ServiceDescriptor item)
        {
            if (item.ServiceType != typeof(IConfigureOptions<HttpClientFactoryOptions>))
            {
                _services.Add(item);
                return;
            }

            if (_isDefault)
            {
                // Insert IConfigureOptions<HttpClientFactoryOptions> services into the collection before named config descriptors.
                // This ensures they run and apply configuration first. Configuration for named clients run afterwards.
                if (_tracker.InsertDefaultsAfterDescriptor != null &&
                    _services.IndexOf(_tracker.InsertDefaultsAfterDescriptor) is var index && index != -1)
                {
                    index++;
                    _services.Insert(index, item);
                }
                else
                {
                    _services.Add(item);
                }

                _tracker.InsertDefaultsAfterDescriptor = item;
            }
            else
            {
                // Track the location of where the first named config descriptor was added.
                _tracker.InsertDefaultsAfterDescriptor ??= _services.Last();

                _services.Add(item);
            }
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
