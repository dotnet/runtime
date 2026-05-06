// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// The result of <see cref="ActivatorUtilities.CreateFactory(Type, Type[])"/>.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to get service arguments from.</param>
    /// <param name="arguments">Additional constructor arguments.</param>
    /// <returns>The instantiated type.</returns>
    public delegate object ObjectFactory(IServiceProvider serviceProvider, object?[]? arguments);
}
