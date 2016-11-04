// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.ComponentModel.Design
{
    /// <summary>
    ///     This interface provides a container for services.  A service container
    ///     is, by definition, a service provider.  In addition to providing services
    ///     it also provides a mechanism for adding and removing services.
    /// </summary>

    public interface IServiceContainer : IServiceProvider
    {
        /// <summary>
        ///     Adds the given service to the service container.
        /// </summary>
        void AddService(Type serviceType, object serviceInstance);

        /// <summary>
        ///     Adds the given service to the service container.
        /// </summary>
        void AddService(Type serviceType, object serviceInstance, bool promote);

        /// <summary>
        ///     Adds the given service to the service container.
        /// </summary>
        void AddService(Type serviceType, ServiceCreatorCallback callback);

        /// <summary>
        ///     Adds the given service to the service container.
        /// </summary>
        void AddService(Type serviceType, ServiceCreatorCallback callback, bool promote);

        /// <summary>
        ///     Removes the given service type from the service container.
        /// </summary>
        void RemoveService(Type serviceType);

        /// <summary>
        ///     Removes the given service type from the service container.
        /// </summary>
        void RemoveService(Type serviceType, bool promote);
    }
}

