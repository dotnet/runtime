// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Returns the result of <see cref="M:Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateFactory``1(System.Type[])" />, which is a delegate that specifies a factory method to call to instantiate an instance of type <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The type of the instance that's returned.</typeparam>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to get service arguments from.</param>
    /// <param name="arguments">Additional constructor arguments.</param>
    /// <returns>An instance of type <typeparamref name="T" />.</returns>
    public delegate T ObjectFactory<T>(IServiceProvider serviceProvider, object?[]? arguments);
}
