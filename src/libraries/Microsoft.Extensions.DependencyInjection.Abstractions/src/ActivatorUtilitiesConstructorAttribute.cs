// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Marks the constructor to be used when activating type using <see cref="ActivatorUtilities"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public class ActivatorUtilitiesConstructorAttribute : Attribute
    {
    }
}
