// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    public static class MetricsBuilderExtensions
    {
        public static IMetricsBuilder AddListener<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this IMetricsBuilder builder) where T : class, IMetricsListener
        {
            ThrowHelper.ThrowIfNull(builder);
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IMetricsListener, T>());
            return builder;
        }

        public static IMetricsBuilder AddListener(this IMetricsBuilder builder, IMetricsListener listener)
        {
            ThrowHelper.ThrowIfNull(builder);
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton(listener));
            return builder;
        }

        public static IMetricsBuilder ClearListeners(this IMetricsBuilder builder)
        {
            ThrowHelper.ThrowIfNull(builder);
            builder.Services.RemoveAll<IMetricsListener>();
            return builder;
        }
    }
}
