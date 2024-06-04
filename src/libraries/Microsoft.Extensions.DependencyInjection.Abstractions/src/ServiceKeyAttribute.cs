// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// ServiceKeyAttribute can be specified on a parameter to inject the key that was used for
    /// registration/resolution.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class ServiceKeyAttribute : Attribute
    {
    }
}
