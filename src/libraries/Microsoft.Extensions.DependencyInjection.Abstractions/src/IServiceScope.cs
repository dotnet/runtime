// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Defines a disposable service scope.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="System.IDisposable.Dispose"/> method ends the scope lifetime. Once <see cref="System.IDisposable.Dispose"/>
    /// is called, any scoped services and any transient services that have been resolved from
    /// <see cref="Microsoft.Extensions.DependencyInjection.IServiceScope.ServiceProvider"/> will be
    /// disposed.
    /// </para>
    /// <para>
    /// If the scope implementation also implements <see cref="System.IAsyncDisposable"/>, prefer calling
    /// <see cref="System.IAsyncDisposable.DisposeAsync"/> over <see cref="System.IDisposable.Dispose"/>. If
    /// any resolved service implements <see cref="System.IAsyncDisposable"/> but not <see cref="System.IDisposable"/>,
    /// calling <see cref="System.IDisposable.Dispose"/> will throw an <see cref="System.InvalidOperationException"/>
    /// (or an <see cref="System.AggregateException"/> if multiple such services are resolved).
    /// Consider using <see cref="AsyncServiceScope"/> to ensure <see cref="System.IAsyncDisposable.DisposeAsync"/> is always called.
    /// </para>
    /// </remarks>
    public interface IServiceScope : IDisposable
    {
        /// <summary>
        /// Gets the <see cref="System.IServiceProvider"/> used to resolve dependencies from the scope.
        /// </summary>
        IServiceProvider ServiceProvider { get; }
    }
}
