// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging
{
    internal readonly struct MessageLogger
    {
        public MessageLogger(ILogger logger, string category, Type providerType, LogLevel? minLevel, Func<string, string, LogLevel, bool> filter)
        {
            Logger = logger;
            Category = category;
            ProviderType = providerType;
            MinLevel = minLevel;
            Filter = filter;
        }

        public ILogger Logger { get; }

        public string Category { get; }

        public Type ProviderType { get; }

        public LogLevel? MinLevel { get; }

        public Func<string, string, LogLevel, bool> Filter { get; }

        public bool IsEnabled(LogLevel level)
        {
            if (MinLevel != null && level < MinLevel)
            {
                return false;
            }

            if (Filter != null)
            {
                return Filter(ProviderType.FullName, Category, level);
            }

            return true;
        }
    }

    internal readonly struct ScopeLogger
    {
        public ScopeLogger(ILogger logger, IExternalScopeProvider externalScopeProvider)
        {
            Logger = logger;
            ExternalScopeProvider = externalScopeProvider;
        }

        public ILogger Logger { get; }

        public IExternalScopeProvider ExternalScopeProvider { get; }

        public IDisposable CreateScope<TState>(TState state)
        {
            if (ExternalScopeProvider != null)
            {
                return ExternalScopeProvider.Push(state);
            }
            return Logger.BeginScope<TState>(state);
        }
    }

    internal readonly struct LoggerInformation
    {
        public LoggerInformation(ILoggerProvider provider, string category) : this()
        {
            ProviderType = provider.GetType();
            Logger = provider.CreateLogger(category);
            Category = category;
            ExternalScope = provider is ISupportExternalScope;
        }

        public ILogger Logger { get; }

        public string Category { get; }

        public Type ProviderType { get; }

        public bool ExternalScope { get; }
    }
}