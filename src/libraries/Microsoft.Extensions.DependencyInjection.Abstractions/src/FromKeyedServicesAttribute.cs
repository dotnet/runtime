// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyInjection
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class FromKeyedServicesAttribute : Attribute
    {
        public FromKeyedServicesAttribute(object key) => Key = key;

        public object Key { get; }
    }
}
