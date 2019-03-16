// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging.Configuration
{
    /// <summary>
    /// Provides a set of helpers to initialize options objects from logger provider configuration.
    /// </summary>
    public static class LoggerProviderOptions
    {
        /// <summary>
        /// Indicates that settings for <typeparamref name="TProvider"/> should be loaded into <typeparamref name="TOptions"/> type.
        /// </summary>
        /// <typeparam name="TOptions">The options class </typeparam>
        /// <typeparam name="TProvider">The provider class</typeparam>
        public static void RegisterProviderOptions<TOptions, TProvider>(IServiceCollection services) where TOptions : class
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<TOptions>, LoggerProviderConfigureOptions<TOptions, TProvider>>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IOptionsChangeTokenSource<TOptions>, LoggerProviderOptionsChangeTokenSource<TOptions, TProvider>>());
        }
    }
}