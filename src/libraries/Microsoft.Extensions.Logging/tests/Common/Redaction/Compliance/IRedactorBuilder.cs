// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Compliance.Redaction;

/// <summary>
/// Adds redactors to the application.
/// </summary>
public interface IRedactionBuilder
{
    /// <summary>
    /// Gets the service collection into which the redactor instances are registered.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Sets the redactor to use for a set of data classes.
    /// </summary>
    /// <typeparam name="T">Redactor type.</typeparam>
    /// <param name="classifications">The data classes for which the redactor type should be used.</param>
    /// <returns>The value of this instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="classifications" /> is <see langword="null" />.</exception>
    IRedactionBuilder SetRedactor<T>(params DataClassification[] classifications)
        where T : Redactor;

    /// <summary>
    /// Sets the redactor to use when processing classified data for which no specific redactor has been registered.
    /// </summary>
    /// <typeparam name="T">Redactor type.</typeparam>
    /// <returns>The value of this instance.</returns>
    IRedactionBuilder SetFallbackRedactor<T>()
        where T : Redactor;
}
