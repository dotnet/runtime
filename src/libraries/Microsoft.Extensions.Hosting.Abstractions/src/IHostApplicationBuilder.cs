// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Represents a hosted applications and services builder which helps manage configuration, logging, lifetime, and more.
/// </summary>
public interface IHostApplicationBuilder
{
    /// <summary>
    /// Gets a central location for sharing state between components during the host building process.
    /// </summary>
    IDictionary<object, object> Properties { get; }

    /// <summary>
    /// Gets the set of key/value configuration properties.
    /// </summary>
    /// <remarks>
    /// This can be mutated by adding more configuration sources, which will update its current view.
    /// </remarks>
    IConfigurationManager Configuration { get; }

    /// <summary>
    /// Gets the information about the hosting environment an application is running in.
    /// </summary>
    IHostEnvironment Environment { get; }

    /// <summary>
    /// Gets a collection of logging providers for the application to compose. This is useful for adding new logging providers.
    /// </summary>
    ILoggingBuilder Logging { get; }

    /// <summary>
    /// Allows enabling metrics and directing their output.
    /// </summary>
    IMetricsBuilder Metrics { get; }

    /// <summary>
    /// Gets a collection of services for the application to compose. This is useful for adding user provided or framework provided services.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Registers a <see cref="IServiceProviderFactory{TContainerBuilder}" /> instance to be used to create the <see cref="IServiceProvider" />.
    /// </summary>
    /// <param name="factory">The factory object that can create the <typeparamref name="TContainerBuilder"/> and <see cref="IServiceProvider"/>.</param>
    /// <param name="configure">
    /// A delegate used to configure the <typeparamref name="TContainerBuilder" />. This can be used to configure services using
    /// APIS specific to the <see cref="IServiceProviderFactory{TContainerBuilder}" /> implementation.
    /// </param>
    /// <typeparam name="TContainerBuilder">The type of builder provided by the <see cref="IServiceProviderFactory{TContainerBuilder}" />.</typeparam>
    /// <remarks>
    /// <para>
    /// The <see cref="IServiceProvider"/> is created when this builder is built and so the delegate provided
    /// by <paramref name="configure"/> will run after all other services have been registered.
    /// </para>
    /// <para>
    /// Multiple calls to <see cref="ConfigureContainer{TContainerBuilder}(IServiceProviderFactory{TContainerBuilder}, Action{TContainerBuilder})"/> will replace
    /// the previously stored <paramref name="factory"/> and <paramref name="configure"/> delegate.
    /// </para>
    /// </remarks>
    void ConfigureContainer<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory, Action<TContainerBuilder>? configure = null) where TContainerBuilder : notnull;
}
