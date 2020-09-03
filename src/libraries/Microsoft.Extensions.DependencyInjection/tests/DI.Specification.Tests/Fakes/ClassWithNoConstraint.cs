﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.DependencyInjection.Specification.Fakes
{
    public class ClassWithNoConstraints<T> : IFakeOpenGenericService<T>
    {
        public T Value { get; } = default;
    }
}
