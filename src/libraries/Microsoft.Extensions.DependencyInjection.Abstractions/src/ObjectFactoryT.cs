// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// The result of <see cref="ActivatorUtilities.CreateFactory{T}"/>. A delegate to specify a factory method to call to instantiate an instance of type `T`
    /// </summary>
    /// <typeparam name="T">The type of the instance being returned</typeparam>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to get service arguments from.</param>
    /// <param name="arguments">Additional constructor arguments.</param>
    /// <returns>An instance of T</returns>
    public delegate T ObjectFactory<T>(IServiceProvider serviceProvider, object?[]? arguments);
}
