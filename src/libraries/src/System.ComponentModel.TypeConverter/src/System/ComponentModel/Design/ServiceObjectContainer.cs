// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Design
{
    using Microsoft.Win32;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Security.Permissions;

    /// <summary>
    ///     This is a simple implementation of IServiceContainer.
    /// </summary>
    public class ServiceContainer : IServiceContainer, IDisposable
    {
        private ServiceCollection<object> _services;
        private IServiceProvider _parentProvider;
        private static Type[] s_defaultServices = new Type[] { typeof(IServiceContainer), typeof(ServiceContainer) };

        private static TraceSwitch s_TRACESERVICE = new TraceSwitch("TRACESERVICE", "ServiceProvider: Trace service provider requests.");

        /// <summary>
        ///     Creates a new service object container.  
        /// </summary>
        public ServiceContainer()
        {
        }

        /// <summary>
        ///     Creates a new service object container.  
        /// </summary>
        public ServiceContainer(IServiceProvider parentProvider)
        {
            _parentProvider = parentProvider;
        }

        /// <summary>
        ///     Retrieves the parent service container, or null
        ///     if there is no parent container.
        /// </summary>
        private IServiceContainer Container
        {
            get
            {
                IServiceContainer container = null;
                if (_parentProvider != null)
                {
                    container = (IServiceContainer)_parentProvider.GetService(typeof(IServiceContainer));
                }
                return container;
            }
        }

        /// <summary>
        ///     This property returns the default services that are implemented directly on this IServiceContainer.
        ///     the default implementation of this property is to return the IServiceContainer and ServiceContainer
        ///     types.  You may override this property and return your own types, modifying the default behavior
        ///     of GetService.
        /// </summary>
        protected virtual Type[] DefaultServices => s_defaultServices;

        /// <summary>
        ///     Our collection of services.  The service collection is demand
        ///     created here.
        /// </summary>
        private ServiceCollection<object> Services => _services ?? (_services = new ServiceCollection<object>());

        /// <summary>
        ///     Adds the given service to the service container.
        /// </summary>
        public void AddService(Type serviceType, object serviceInstance)
        {
            AddService(serviceType, serviceInstance, false);
        }

        /// <summary>
        ///     Adds the given service to the service container.
        /// </summary>
        public virtual void AddService(Type serviceType, object serviceInstance, bool promote)
        {
            Debug.WriteLineIf(s_TRACESERVICE.TraceVerbose, $"Adding service (instance) {serviceType.Name}.  Promoting: {promote.ToString()}");
            if (promote)
            {
                IServiceContainer container = Container;
                if (container != null)
                {
                    Debug.WriteLineIf(s_TRACESERVICE.TraceVerbose, "Promoting to container");
                    container.AddService(serviceType, serviceInstance, promote);
                    return;
                }
            }

            // We're going to add this locally.  Ensure that the service instance
            // is correct.
            //
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            if (serviceInstance == null) throw new ArgumentNullException(nameof(serviceInstance));
            if (!(serviceInstance is ServiceCreatorCallback) && !serviceInstance.GetType().IsCOMObject && !serviceType.IsInstanceOfType(serviceInstance))
            {
                throw new ArgumentException(SR.Format(SR.ErrorInvalidServiceInstance, serviceType.FullName));
            }

            if (Services.ContainsKey(serviceType))
            {
                throw new ArgumentException(SR.Format(SR.ErrorServiceExists, serviceType.FullName), nameof(serviceType));
            }

            Services[serviceType] = serviceInstance;
        }

        /// <summary>
        ///     Adds the given service to the service container.
        /// </summary>
        public void AddService(Type serviceType, ServiceCreatorCallback callback)
        {
            AddService(serviceType, callback, false);
        }

        /// <summary>
        ///     Adds the given service to the service container.
        /// </summary>
        public virtual void AddService(Type serviceType, ServiceCreatorCallback callback, bool promote)
        {
            Debug.WriteLineIf(s_TRACESERVICE.TraceVerbose, $"Adding service (callback) {serviceType.Name}.  Promoting: {promote.ToString()}");
            if (promote)
            {
                IServiceContainer container = Container;
                if (container != null)
                {
                    Debug.WriteLineIf(s_TRACESERVICE.TraceVerbose, "Promoting to container");
                    container.AddService(serviceType, callback, promote);
                    return;
                }
            }

            // We're going to add this locally.  Ensure that the service instance
            // is correct.
            //
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            if (Services.ContainsKey(serviceType))
            {
                throw new ArgumentException(SR.Format(SR.ErrorServiceExists, serviceType.FullName), nameof(serviceType));
            }

            Services[serviceType] = callback;
        }

        /// <summary>
        ///     Disposes this service container.  This also walks all instantiated services within the container
        ///     and disposes any that implement IDisposable, and clears the service list.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        ///     Disposes this service container.  This also walks all instantiated services within the container
        ///     and disposes any that implement IDisposable, and clears the service list.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServiceCollection<object> serviceCollection = _services;
                _services = null;
                if (serviceCollection != null)
                {
                    foreach (object o in serviceCollection.Values)
                    {
                        if (o is IDisposable)
                        {
                            ((IDisposable)o).Dispose();
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Retrieves the requested service.
        /// </summary>
        public virtual object GetService(Type serviceType)
        {
            object service = null;

            Debug.WriteLineIf(s_TRACESERVICE.TraceVerbose, $"Searching for service {serviceType.Name}");

            // Try locally.  We first test for services we
            // implement and then look in our service collection.
            //
            Type[] defaults = DefaultServices;
            for (int idx = 0; idx < defaults.Length; idx++)
            {
                if (serviceType.IsEquivalentTo(defaults[idx]))
                {
                    service = this;
                    break;
                }
            }

            if (service == null)
            {
                Services.TryGetValue(serviceType, out service);
            }

            // Is the service a creator delegate?
            //
            if (service is ServiceCreatorCallback)
            {
                Debug.WriteLineIf(s_TRACESERVICE.TraceVerbose, "Encountered a callback. Invoking it");
                service = ((ServiceCreatorCallback)service)(this, serviceType);
                Debug.WriteLineIf(s_TRACESERVICE.TraceVerbose, $"Callback return object: {(service == null ? "(null)" : service.ToString())}");
                if (service != null && !service.GetType().IsCOMObject && !serviceType.IsInstanceOfType(service))
                {
                    // Callback passed us a bad service.  NULL it, rather than throwing an exception.
                    // Callers here do not need to be prepared to handle bad callback implemetations.
                    Debug.Fail($"Object {service.GetType().Name} was returned from a service creator callback but it does not implement the registered type of {serviceType.Name}");
                    Debug.WriteLineIf(s_TRACESERVICE.TraceVerbose, "**** Object does not implement service interface");
                    service = null;
                }

                // And replace the callback with our new service.
                //
                Services[serviceType] = service;
            }

            if (service == null && _parentProvider != null)
            {
                Debug.WriteLineIf(s_TRACESERVICE.TraceVerbose, "Service unresolved.  Trying parent");
                service = _parentProvider.GetService(serviceType);
            }

#if DEBUG
            if (s_TRACESERVICE.TraceVerbose && service == null)
            {
                Debug.WriteLine("******************************************");
                Debug.WriteLine("FAILED to resolve service " + serviceType.Name);
                Debug.WriteLine("AT: " + Environment.StackTrace);
                Debug.WriteLine("******************************************");
            }
#endif

            return service;
        }

        /// <summary>
        ///     Removes the given service type from the service container.
        /// </summary>
        public void RemoveService(Type serviceType)
        {
            RemoveService(serviceType, false);
        }

        /// <summary>
        ///     Removes the given service type from the service container.
        /// </summary>
        public virtual void RemoveService(Type serviceType, bool promote)
        {
            Debug.WriteLineIf(s_TRACESERVICE.TraceVerbose, $"Removing service: {serviceType.Name}, Promote: {promote.ToString()}");
            if (promote)
            {
                IServiceContainer container = Container;
                if (container != null)
                {
                    Debug.WriteLineIf(s_TRACESERVICE.TraceVerbose, "Invoking parent container");
                    container.RemoveService(serviceType, promote);
                    return;
                }
            }

            // We're going to remove this from our local list.
            //
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            Services.Remove(serviceType);
        }

        /// <summary>
        /// Use this collection to store mapping from the Type of a service to the object that provides it in a way
        /// that is aware of embedded types.   The comparer for this collection will call Type.IsEquivalentTo(...)
        /// instead of doing a reference comparison which will fail in type embedding scenarios.  To speed the lookup
        /// performance we will use hash code of Type.FullName.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private sealed class ServiceCollection<T> : Dictionary<Type, T>
        {
            private static EmbeddedTypeAwareTypeComparer s_serviceTypeComparer = new EmbeddedTypeAwareTypeComparer();

            private sealed class EmbeddedTypeAwareTypeComparer : IEqualityComparer<Type>
            {
                #region IEqualityComparer<Type> Members

                public bool Equals(Type x, Type y)
                {
                    return x.IsEquivalentTo(y);
                }

                public int GetHashCode(Type obj)
                {
                    return obj.FullName.GetHashCode();
                }

                #endregion
            }

            public ServiceCollection() : base(s_serviceTypeComparer)
            {
            }
        }
    }
}

